using AeroLicense.Domain.Common;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Domain.Entities;

/// <summary>Yetkili eğitim kuruluşunun kaydettiği, tamamlanmış bir eğitim ve sınav sonucu.</summary>
public sealed class TrainingRecord : Entity
{
    public const int PassingScore = 70;

    private TrainingRecord() { } // EF Core için

    public TrainingRecord(Guid applicantId, Guid trainingOrgId, Guid recordedByUserId,
        LicenseType licenseType, DateTime completedAtUtc, int examScore, DateTime nowUtc)
    {
        if (examScore is < 0 or > 100)
            throw new DomainException("Sınav puanı 0 ile 100 arasında olmalıdır.");
        if (completedAtUtc > nowUtc)
            throw new DomainException("Tamamlanma tarihi gelecekte olamaz.");

        ApplicantId = applicantId;
        TrainingOrgId = trainingOrgId;
        RecordedByUserId = recordedByUserId;
        LicenseType = licenseType;
        CompletedAtUtc = completedAtUtc;
        ExamScore = examScore;
        RecordedAtUtc = nowUtc;
    }

    public Guid ApplicantId { get; private set; }
    public User Applicant { get; private set; } = null!;
    public Guid TrainingOrgId { get; private set; }
    public Organization TrainingOrg { get; private set; } = null!;
    public Guid RecordedByUserId { get; private set; }

    /// <summary>Eğitimin hazırladığı lisans türü. Başvuru bu türle eşleşen başarılı eğitim ister.</summary>
    public LicenseType LicenseType { get; private set; }

    public DateTime CompletedAtUtc { get; private set; }
    public int ExamScore { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    // Saklanmaz, puandan hesaplanır: "başarılı" bilgisi ile puan asla çelişemez.
    public bool IsPassed => ExamScore >= PassingScore;
}
