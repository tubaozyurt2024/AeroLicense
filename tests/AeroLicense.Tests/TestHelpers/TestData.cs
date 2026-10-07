using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Infrastructure.Persistence;

namespace AeroLicense.Tests.TestHelpers;

public static class TestData
{
    public static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    public static User Applicant(string email = "pilot@aerolicense.test") =>
        new(email, "Test Pilot", UserRole.Applicant, Now, nationalId: "99999999999");

    /// <summary>
    /// License'ın constructor'ı internal: lisans sadece onay akışıyla doğar. Test de aynı yoldan geçer.
    /// Eğitim kuruluşu gerektirmemek için eğitim kaydının kuruluşu olarak geçici bir kuruluş eklenir.
    /// </summary>
    public static License IssueLicense(AppDbContext context, User pilot, User inspector, DateTime issuedAtUtc,
        LicenseType type = LicenseType.CPL)
    {
        var ato = new Organization("ATO " + Guid.NewGuid().ToString("N")[..6], OrganizationType.TrainingOrg, Guid.NewGuid().ToString("N")[..8]);
        var training = new TrainingRecord(pilot.Id, ato.Id, inspector.Id, type, issuedAtUtc.AddDays(-10), 90, issuedAtUtc);
        var application = new LicenseApplication(pilot.Id, type, issuedAtUtc);
        application.Submit(training, issuedAtUtc);
        application.StartReview(inspector.Id, issuedAtUtc);
        var license = application.Approve(inspector.Id, issuedAtUtc);

        context.Organizations.Add(ato);
        context.TrainingRecords.Add(training);
        context.LicenseApplications.Add(application);
        context.Licenses.Add(license);
        return license;
    }
}
