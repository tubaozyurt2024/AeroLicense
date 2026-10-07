using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Common.Idempotency;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.FlightLogs;

public interface IFlightLogService
{
    Task<CreateFlightLogResult> CreateAsync(CreateFlightLogRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    Task<PilotSummaryDto> GetPilotSummaryAsync(Guid pilotId, CancellationToken cancellationToken);
    Task<PagedResult<FlightLogListItemDto>> ListAsync(FlightLogQuery query, CancellationToken cancellationToken);
}

public sealed class FlightLogService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IFlightLogService
{
    public async Task<CreateFlightLogResult> CreateAsync(CreateFlightLogRequest request, string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!CreateFlightLogRequestValidator.IsValidIdempotencyKey(idempotencyKey))
            throw new ValidationException([new ValidationFailure("Idempotency-Key",
                $"Idempotency-Key başlığı zorunludur (en fazla {CreateFlightLogRequestValidator.MaxIdempotencyKeyLength} karakter; harf, rakam, '-', '_').")]);

        var airlineId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Kullanıcı bir havayoluna bağlı değil.");
        var requestHash = Hash(request);

        // 1) Bu anahtar daha önce işlendiyse yeni kayıt oluşturmadan ilk sonucu dön.
        if (await ReplayAsync(airlineId, idempotencyKey!, requestHash, cancellationToken) is { } replay)
            return replay;

        // 2) Pilotun uçuş anında geçerli lisansı olmalı (sonradan geçerliliğini yitirmiş olması sorun değil).
        var license = await db.Licenses.SingleOrDefaultAsync(l => l.LicenseNumber == request.PilotLicenseNumber, cancellationToken);
        if (license is null || !license.IsValidAt(request.DepartureAtUtc))
            throw new DomainException("Pilotun uçuş tarihinde geçerli bir lisansı bulunmuyor.");

        // 3) Aynı pilot aynı anda iki uçuşta olamaz (hangi havayolundan gelirse gelsin).
        //    Koşul FlightLog.Overlaps ile aynı: başlangıç1 < bitiş2 VE başlangıç2 < bitiş1.
        if (await db.FlightLogs.AnyAsync(f => f.PilotId == license.HolderId
                && f.DepartureAtUtc < request.ArrivalAtUtc && request.DepartureAtUtc < f.ArrivalAtUtc, cancellationToken))
            throw new DomainException("Pilotun bu zaman aralığıyla çakışan başka bir uçuş kaydı var.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var flightLog = new FlightLog(license.HolderId, license.Id, airlineId, currentUser.UserId, request.FlightNumber,
            request.DepartureAirport, request.ArrivalAirport, request.DepartureAtUtc, request.ArrivalAtUtc, now);

        db.FlightLogs.Add(flightLog);
        db.IdempotencyRecords.Add(new IdempotencyRecord(airlineId, idempotencyKey!, requestHash, flightLog.Id, now));
        db.AuditLogs.Add(new AuditLog(currentUser.UserId, AuditActions.FlightLogCreated, nameof(FlightLog), flightLog.Id, now,
            $"{flightLog.FlightNumber} {flightLog.DepartureAirport}-{flightLog.ArrivalAirport}"));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Aynı anahtarla eşzamanlı iki istek: ikisi de 1. adımı geçti, unique index ikincisini durdurdu.
            // Kazanan isteğin sonucunu dönmek, istemci açısından tekrar denemeyle aynı davranıştır.
            if (await ReplayAsync(airlineId, idempotencyKey!, requestHash, cancellationToken) is { } raced)
                return raced;
            throw;
        }

        return new CreateFlightLogResult(ToDto(flightLog, license.LicenseNumber), Replayed: false);
    }

    public async Task<PilotSummaryDto> GetPilotSummaryAsync(Guid pilotId, CancellationToken cancellationToken)
    {
        // Pilot sadece kendi özetini görür; başkasının id'si için 404 (IDOR'a karşı, varlık da sızmaz).
        if (currentUser.Role == UserRole.Applicant && currentUser.UserId != pilotId)
            throw new NotFoundException("Pilot bulunamadı.");
        if (!await db.Users.AnyAsync(u => u.Id == pilotId && u.Role == UserRole.Applicant, cancellationToken))
            throw new NotFoundException("Pilot bulunamadı.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var since30 = now.AddDays(-30);
        var since90 = now.AddDays(-90);

        // Tek SQL, tek geçiş: SUM(CASE WHEN ...) ile üç toplam aynı anda hesaplanır. Satırlar belleğe
        // çekilmez; (PilotId, DepartureAtUtc) INCLUDE (DurationMinutes, ArrivalAtUtc) index'i sayesinde
        // PostgreSQL tabloya hiç gitmeden (index-only scan) sonuca ulaşabilir.
        var totals = await db.FlightLogs
            .Where(f => f.PilotId == pilotId)
            .GroupBy(f => f.PilotId)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(f => f.DurationMinutes),
                Last30 = g.Sum(f => f.DepartureAtUtc >= since30 ? f.DurationMinutes : 0),
                Last90 = g.Sum(f => f.DepartureAtUtc >= since90 ? f.DurationMinutes : 0),
                LastFlight = g.Max(f => f.ArrivalAtUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return totals is null
            ? new PilotSummaryDto(pilotId, 0, 0, 0, 0, null)
            : new PilotSummaryDto(pilotId, totals.Count, Hours(totals.Total), Hours(totals.Last30), Hours(totals.Last90),
                totals.LastFlight);
    }

    public async Task<PagedResult<FlightLogListItemDto>> ListAsync(FlightLogQuery query, CancellationToken cancellationToken)
    {
        // Kayıt düzeyinde yetki: havayolu sadece kendi gönderdiklerini, denetçi hepsini görür.
        var flights = currentUser.Role switch
        {
            UserRole.Inspector => db.FlightLogs,
            UserRole.Airline => db.FlightLogs.Where(f => f.AirlineId == currentUser.OrganizationId),
            _ => throw new ForbiddenException("Bu rol uçuş kayıtlarını listeleyemez.")
        };

        if (query.Suspicious)
            // FlightLog.IsLongDuration / IsLateReport ile aynı kural; hesaplanan özellikler SQL'e çevrilemediği için açık yazıldı.
            flights = flights.Where(f => f.DurationMinutes >= FlightLog.SuspiciousDurationMinutes
                || f.RecordedAtUtc > f.ArrivalAtUtc.AddDays(FlightLog.LateReportDays));
        if (!string.IsNullOrWhiteSpace(query.PilotLicenseNumber))
            flights = flights.Where(f => f.License.LicenseNumber == query.PilotLicenseNumber);
        if (query.FromUtc is { } from) flights = flights.Where(f => f.DepartureAtUtc >= from);
        if (query.ToUtc is { } to) flights = flights.Where(f => f.DepartureAtUtc <= to);

        var totalCount = await flights.CountAsync(cancellationToken);
        var rows = await flights
            .OrderByDescending(f => f.DepartureAtUtc).ThenByDescending(f => f.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(f => new
            {
                f.Id, f.License.LicenseNumber, PilotName = f.Pilot.FullName, AirlineCode = f.Airline.Code, f.FlightNumber,
                f.DepartureAirport, f.ArrivalAirport, f.DepartureAtUtc, f.ArrivalAtUtc, f.DurationMinutes, f.RecordedAtUtc,
                LateReport = f.RecordedAtUtc > f.ArrivalAtUtc.AddDays(FlightLog.LateReportDays)
            })
            .ToListAsync(cancellationToken);

        var showPilotName = currentUser.Role == UserRole.Inspector;
        var items = rows.Select(r => new FlightLogListItemDto(r.Id, r.LicenseNumber, showPilotName ? r.PilotName : null,
            r.AirlineCode, r.FlightNumber, r.DepartureAirport, r.ArrivalAirport, r.DepartureAtUtc, r.ArrivalAtUtc,
            r.DurationMinutes, r.RecordedAtUtc, Reasons(r.DurationMinutes, r.LateReport))).ToList();

        return new PagedResult<FlightLogListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static IReadOnlyList<SuspicionReason> Reasons(int durationMinutes, bool lateReport)
    {
        var reasons = new List<SuspicionReason>(2);
        if (durationMinutes >= FlightLog.SuspiciousDurationMinutes) reasons.Add(SuspicionReason.LongDuration);
        if (lateReport) reasons.Add(SuspicionReason.LateReport);
        return reasons;
    }

    private async Task<CreateFlightLogResult?> ReplayAsync(Guid airlineId, string key, string requestHash,
        CancellationToken cancellationToken)
    {
        var record = await db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(r => r.ClientId == airlineId && r.Key == key, cancellationToken);
        if (record is null) return null;

        // Aynı anahtar farklı içerikle: istemci hatası. Sessizce eski sonucu dönmek veriyi kaybettirirdi.
        if (record.RequestHash != requestHash)
            throw new DomainException("Bu Idempotency-Key farklı içerikli bir istek için zaten kullanılmış.");

        var dto = await db.FlightLogs.AsNoTracking()
            .Where(f => f.Id == record.ResourceId)
            .Select(f => new FlightLogDto(f.Id, f.License.LicenseNumber, f.FlightNumber, f.DepartureAirport,
                f.ArrivalAirport, f.DepartureAtUtc, f.ArrivalAtUtc, f.DurationMinutes, f.RecordedAtUtc))
            .SingleAsync(cancellationToken);
        return new CreateFlightLogResult(dto, Replayed: true);
    }

    // Record'un JSON'u deterministik (alan sırası sabit), bu yüzden aynı içerik her zaman aynı özeti verir.
    private static string Hash(CreateFlightLogRequest request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));

    private static decimal Hours(int minutes) => Math.Round(minutes / 60m, 1);

    private static FlightLogDto ToDto(FlightLog f, string licenseNumber) =>
        new(f.Id, licenseNumber, f.FlightNumber, f.DepartureAirport, f.ArrivalAirport, f.DepartureAtUtc, f.ArrivalAtUtc,
            f.DurationMinutes, f.RecordedAtUtc);
}
