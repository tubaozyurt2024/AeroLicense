using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.Licenses;

public interface ILicenseService
{
    Task<PagedResult<LicenseDto>> ListAsync(LicenseQuery query, CancellationToken cancellationToken);
    Task<byte[]> GetQrCodePngAsync(Guid id, CancellationToken cancellationToken);
    Task<LicenseDto> RevokeAsync(Guid id, RevokeLicenseRequest request, CancellationToken cancellationToken);
}

public sealed class LicenseService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IVerificationLinks verificationLinks,
    IQrCodeGenerator qrCodeGenerator) : ILicenseService
{
    public async Task<PagedResult<LicenseDto>> ListAsync(LicenseQuery query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var licenses = VisibleLicenses();

        // Durum saklanmadığı için filtre, durumu belirleyen kolonlar üzerinden SQL'e çevrilir.
        licenses = query.Status switch
        {
            LicenseStatus.Active => licenses.Where(l => l.RevokedAtUtc == null && l.ExpiresAtUtc > now),
            LicenseStatus.Expired => licenses.Where(l => l.RevokedAtUtc == null && l.ExpiresAtUtc <= now),
            LicenseStatus.Revoked => licenses.Where(l => l.RevokedAtUtc != null),
            _ => licenses
        };

        var totalCount = await licenses.CountAsync(cancellationToken);
        var rows = await licenses
            .OrderByDescending(l => l.IssuedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<LicenseDto>(rows.Select(r => ToDto(r, now)).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<byte[]> GetQrCodePngAsync(Guid id, CancellationToken cancellationToken)
    {
        var code = await VisibleLicenses()
            .Where(l => l.Id == id)
            .Select(l => db.LicenseDocuments.Where(d => d.LicenseId == l.Id)
                .OrderByDescending(d => d.IssuedAtUtc).Select(d => d.VerificationCode).FirstOrDefault())
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lisans veya belgesi bulunamadı.");

        // QR sadece doğrulama adresini taşır; kişisel veri taşımaz. Kod çalınsa bile sadece maskeli bilgi görülür.
        return qrCodeGenerator.GeneratePng(verificationLinks.For(code));
    }

    public async Task<LicenseDto> RevokeAsync(Guid id, RevokeLicenseRequest request, CancellationToken cancellationToken)
    {
        var license = await db.Licenses.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lisans bulunamadı.");
        var now = timeProvider.GetUtcNow().UtcDateTime;

        license.Revoke(request.Reason, now);
        db.AuditLogs.Add(new AuditLog(currentUser.UserId, AuditActions.LicenseRevoked, nameof(License), license.Id, now));
        await db.SaveChangesAsync(cancellationToken);

        var row = await db.Licenses.Where(l => l.Id == id).Select(Projection).SingleAsync(cancellationToken);
        return ToDto(row, now);
    }

    // Kayıt düzeyinde yetki: lisans sahibi sadece kendi lisanslarını, denetçi hepsini görür.
    private IQueryable<License> VisibleLicenses() => currentUser.Role switch
    {
        UserRole.Applicant => db.Licenses.Where(l => l.HolderId == currentUser.UserId),
        UserRole.Inspector => db.Licenses,
        _ => throw new ForbiddenException("Bu rol lisansları görüntüleyemez.")
    };

    private sealed record LicenseRow(Guid Id, string LicenseNumber, Guid HolderId, string HolderName, LicenseType Type,
        DateTime IssuedAtUtc, DateTime ExpiresAtUtc, DateTime? RevokedAtUtc, string? VerificationCode);

    private System.Linq.Expressions.Expression<Func<License, LicenseRow>> Projection => l => new LicenseRow(
        l.Id, l.LicenseNumber, l.HolderId, l.Holder.FullName, l.Type, l.IssuedAtUtc, l.ExpiresAtUtc, l.RevokedAtUtc,
        db.LicenseDocuments.Where(d => d.LicenseId == l.Id).OrderByDescending(d => d.IssuedAtUtc)
            .Select(d => d.VerificationCode).FirstOrDefault());

    private LicenseDto ToDto(LicenseRow r, DateTime now) =>
        new(r.Id, r.LicenseNumber, r.HolderId, r.HolderName, r.Type, r.IssuedAtUtc, r.ExpiresAtUtc,
            License.StatusOf(r.RevokedAtUtc, r.ExpiresAtUtc, now), r.VerificationCode,
            r.VerificationCode is null ? null : verificationLinks.For(r.VerificationCode));
}
