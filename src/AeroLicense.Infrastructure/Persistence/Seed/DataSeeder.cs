using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AeroLicense.Infrastructure.Persistence.Seed;

/// <summary>
/// Sadece kurgusal demo verisi. E-postalar IANA'nın test için ayırdığı ".test" alan adında,
/// kimlik numaraları gerçek olamayacak şekilde 99999... ile başlıyor.
/// Her bölüm kendi tablosu boşsa çalışır; böylece yeni modül eklendiğinde mevcut veritabanı da tamamlanır.
/// </summary>
public sealed class DataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    LicenseDocumentIssuer documentIssuer,
    TimeProvider timeProvider,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(string demoPassword, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (!await db.Users.AnyAsync(cancellationToken))
            await SeedUsersAsync(demoPassword, now, cancellationToken);

        if (!await db.TrainingRecords.AnyAsync(cancellationToken))
            await SeedTrainingRecordsAsync(now, cancellationToken);

        if (!await db.LicenseApplications.AnyAsync(cancellationToken))
            await SeedApplicationsAsync(now, cancellationToken);

        if (!await db.FlightLogs.AnyAsync(cancellationToken))
            await SeedFlightLogsAsync(now, cancellationToken);

        // Belge modülünden önce oluşmuş lisanslar da doğrulanabilir belgeye kavuşsun.
        await SeedMissingDocumentsAsync(cancellationToken);
    }

    private async Task SeedMissingDocumentsAsync(CancellationToken cancellationToken)
    {
        var licenses = await db.Licenses.Include(l => l.Holder)
            .Where(l => !db.LicenseDocuments.Any(d => d.LicenseId == l.Id))
            .ToListAsync(cancellationToken);
        if (licenses.Count == 0) return;

        foreach (var license in licenses)
            db.LicenseDocuments.Add(documentIssuer.Issue(license, license.Holder.FullName, license.IssuedAtUtc));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed: {Count} lisans için doğrulanabilir belge oluşturuldu", licenses.Count);
    }

    private async Task SeedUsersAsync(string demoPassword, DateTime now, CancellationToken cancellationToken)
    {
        var ato1 = new Organization("Anadolu Uçuş Akademisi (Demo)", OrganizationType.TrainingOrg, "ATO-001");
        var ato2 = new Organization("Gökyüzü Pilot Okulu (Demo)", OrganizationType.TrainingOrg, "ATO-002");
        var airline1 = new Organization("Demo Hava Yolları", OrganizationType.Airline, "XDA");
        var airline2 = new Organization("Kurgu Air", OrganizationType.Airline, "XDB");
        db.Organizations.AddRange(ato1, ato2, airline1, airline2);

        var users = new[]
        {
            new User("inspector@aerolicense.test", "Denetçi Demo", UserRole.Inspector, now),
            new User("egitim1@aerolicense.test", "Eğitim Sorumlusu 1", UserRole.TrainingOrg, now, ato1.Id),
            new User("egitim2@aerolicense.test", "Eğitim Sorumlusu 2", UserRole.TrainingOrg, now, ato2.Id),
            new User("havayolu1@aerolicense.test", "Entegrasyon XDA", UserRole.Airline, now, airline1.Id),
            new User("havayolu2@aerolicense.test", "Entegrasyon XDB", UserRole.Airline, now, airline2.Id),
            new User("pilot1@aerolicense.test", "Pilot Ada Demo", UserRole.Applicant, now, nationalId: "99999999901"),
            new User("pilot2@aerolicense.test", "Pilot Bora Demo", UserRole.Applicant, now, nationalId: "99999999902"),
            new User("pilot3@aerolicense.test", "Pilot Cem Demo", UserRole.Applicant, now, nationalId: "99999999903")
        };
        foreach (var user in users)
            user.SetPasswordHash(passwordHasher.Hash(demoPassword));
        db.Users.AddRange(users);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed: {UserCount} kullanıcı, 4 kuruluş oluşturuldu", users.Length);
    }

    private async Task SeedTrainingRecordsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var users = await db.Users.ToDictionaryAsync(u => u.Email, cancellationToken);
        User Get(string name) => users[$"{name}@aerolicense.test"];

        var ato1 = Get("egitim1");
        var ato2 = Get("egitim2");

        TrainingRecord Record(string pilot, User recorder, LicenseType type, int daysAgo, int score) =>
            new(Get(pilot).Id, recorder.OrganizationId!.Value, recorder.Id, type, now.AddDays(-daysAgo), score, now);

        db.TrainingRecords.AddRange(
            Record("pilot1", ato1, LicenseType.PPL, 400, 88),
            Record("pilot1", ato1, LicenseType.CPL, 30, 82),   // pilot1 CPL başvurusu yapabilir
            Record("pilot2", ato2, LicenseType.CPL, 60, 62),   // başarısız (70 altı)
            Record("pilot2", ato2, LicenseType.CPL, 20, 75),   // tekrar sınavda başarılı
            Record("pilot3", ato1, LicenseType.PPL, 900, 91),
            Record("pilot3", ato2, LicenseType.ATPL, 10, 55)); // başarısız: ATPL başvurusu reddedilmeli

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed: eğitim kayıtları oluşturuldu");
    }

    /// <summary>
    /// Lisanslar da domain akışından geçerek üretilir (Submit → StartReview → Approve); seed verisi
    /// iş kurallarını atlayamaz. Geçmiş tarihler, süresi dolmuş lisans senaryosu içindir.
    /// </summary>
    private async Task SeedApplicationsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var users = await db.Users.ToDictionaryAsync(u => u.Email, cancellationToken);
        var inspector = users["inspector@aerolicense.test"];
        var trainings = await db.TrainingRecords.ToListAsync(cancellationToken);

        LicenseApplication Approved(string pilot, LicenseType type, int daysAgo)
        {
            var applicantId = users[$"{pilot}@aerolicense.test"].Id;
            var training = trainings
                .Where(t => t.ApplicantId == applicantId && t.LicenseType == type && t.IsPassed)
                .MaxBy(t => t.CompletedAtUtc)!;
            var at = now.AddDays(-daysAgo);

            var application = new LicenseApplication(applicantId, type, at);
            application.Submit(training, at);
            application.StartReview(inspector.Id, at.AddDays(1));
            db.Licenses.Add(application.Approve(inspector.Id, at.AddDays(2)));
            return application;
        }

        db.LicenseApplications.AddRange(
            Approved("pilot1", LicenseType.PPL, 390),  // geçerli
            Approved("pilot2", LicenseType.CPL, 15),   // geçerli
            Approved("pilot3", LicenseType.PPL, 880),  // 2 yıl geçmiş: süresi dolmuş lisans
            new LicenseApplication(users["pilot1@aerolicense.test"].Id, LicenseType.CPL, now.AddDays(-1))); // taslak

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed: lisans başvuruları ve lisanslar oluşturuldu");
    }

    private async Task SeedFlightLogsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var users = await db.Users.ToDictionaryAsync(u => u.Email, cancellationToken);
        var licenses = await db.Licenses.ToListAsync(cancellationToken);

        // reportedAfterDays: havayolu kaydı inişten kaç gün sonra gönderdi (normalde aynı gün).
        FlightLog Flight(string pilot, string airline, string flightNumber, string from, string to, int daysAgo,
            int minutes, int reportedAfterDays = 0)
        {
            var pilotId = users[$"{pilot}@aerolicense.test"].Id;
            var recorder = users[$"{airline}@aerolicense.test"];
            var departure = now.Date.AddDays(-daysAgo).AddHours(8);
            var arrival = departure.AddMinutes(minutes);
            var reportedAt = Min(arrival.AddHours(2).AddDays(reportedAfterDays), now);
            var license = licenses.First(l => l.HolderId == pilotId && l.IsValidAt(departure));
            return new FlightLog(pilotId, license.Id, recorder.OrganizationId!.Value, recorder.Id, flightNumber,
                from, to, departure, arrival, reportedAt);
        }

        db.FlightLogs.AddRange(
            Flight("pilot1", "havayolu1", "XDA101", "LTFM", "LTAC", 200, 65),
            Flight("pilot1", "havayolu1", "XDA102", "LTAC", "LTFM", 60, 70),
            Flight("pilot1", "havayolu2", "XDB310", "LTFM", "LTAI", 20, 80),
            Flight("pilot1", "havayolu1", "XDA205", "LTAI", "LTBJ", 3, 55),
            Flight("pilot1", "havayolu1", "XDA150", "LTBJ", "LTFE", 100, 50, reportedAfterDays: 45), // şüpheli: geç bildirim
            Flight("pilot2", "havayolu2", "XDB400", "LTBJ", "LTFM", 10, 60),
            Flight("pilot2", "havayolu2", "XDB11", "LTFM", "KJFK", 12, 13 * 60),                    // şüpheli: 13 saat
            Flight("pilot3", "havayolu2", "XDB900", "LTFM", "LTFE", 800, 90)); // lisansı henüz geçerliyken

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed: uçuş kayıtları oluşturuldu");
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
