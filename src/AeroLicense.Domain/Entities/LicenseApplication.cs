using AeroLicense.Domain.Common;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Domain.Entities;

/// <summary>
/// Lisans başvurusu ve iş akışı: Draft → Submitted → UnderReview → Approved / Rejected.
/// Durum dışarıdan set edilemez; sadece aşağıdaki metotlarla, izin verilen geçişlerle değişir.
/// </summary>
public sealed class LicenseApplication : Entity
{
    // Tek doğruluk kaynağı: hangi durumdan hangisine geçilebilir. README'deki diyagram bununla aynı.
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Draft] = [ApplicationStatus.Submitted],
        [ApplicationStatus.Submitted] = [ApplicationStatus.UnderReview],
        [ApplicationStatus.UnderReview] = [ApplicationStatus.Approved, ApplicationStatus.Rejected],
        [ApplicationStatus.Approved] = [],
        [ApplicationStatus.Rejected] = []
    };

    private LicenseApplication() { } // EF Core için

    public LicenseApplication(Guid applicantId, LicenseType licenseType, DateTime nowUtc)
    {
        ApplicantId = applicantId;
        LicenseType = licenseType;
        Status = ApplicationStatus.Draft;
        CreatedAtUtc = nowUtc;
        Version = Guid.NewGuid();
    }

    public Guid ApplicantId { get; private set; }
    public User Applicant { get; private set; } = null!;
    public LicenseType LicenseType { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Başvurunun dayandığı başarılı eğitim kaydı (gönderimde sabitlenir).</summary>
    public Guid? TrainingRecordId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }

    public Guid? ReviewerId { get; private set; }
    public User? Reviewer { get; private set; }
    public DateTime? ReviewStartedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public License? License { get; private set; }

    /// <summary>
    /// İyimser eşzamanlılık belirteci: her durum değişikliğinde yenilenir. İki denetçi aynı başvuruya
    /// aynı anda karar verirse ikinci kayıt DbUpdateConcurrencyException alır (409).
    /// </summary>
    public Guid Version { get; private set; }

    /// <summary>Henüz sonuçlanmamış durumlar. Aynı türde ikinci açık başvuru engellenir.</summary>
    public static readonly ApplicationStatus[] OpenStatuses =
        [ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.UnderReview];

    public void Submit(TrainingRecord passedTraining, DateTime nowUtc)
    {
        // Kanıt başka bir kişiye veya başka lisans türüne ait olamaz, başarısız olamaz.
        if (passedTraining.ApplicantId != ApplicantId || passedTraining.LicenseType != LicenseType)
            throw new DomainException("Eğitim kaydı bu başvuruyla eşleşmiyor.");
        if (!passedTraining.IsPassed)
            throw new DomainException($"{LicenseType} başvurusu için başarılı (en az {TrainingRecord.PassingScore} puan) eğitim gereklidir.");

        MoveTo(ApplicationStatus.Submitted);
        TrainingRecordId = passedTraining.Id;
        SubmittedAtUtc = nowUtc;
    }

    public void StartReview(Guid inspectorId, DateTime nowUtc)
    {
        MoveTo(ApplicationStatus.UnderReview);
        ReviewerId = inspectorId;
        ReviewStartedAtUtc = nowUtc;
    }

    public License Approve(Guid inspectorId, DateTime nowUtc)
    {
        EnsureReviewer(inspectorId);
        MoveTo(ApplicationStatus.Approved);
        DecidedAtUtc = nowUtc;

        // Onay ile lisans aynı işlemde doğar: "onaylı ama lisanssız" ara durum oluşamaz.
        License = new License(ApplicantId, Id, LicenseType, nowUtc);
        return License;
    }

    public void Reject(Guid inspectorId, string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Ret gerekçesi zorunludur.");

        EnsureReviewer(inspectorId);
        MoveTo(ApplicationStatus.Rejected);
        RejectionReason = reason.Trim();
        DecidedAtUtc = nowUtc;
    }

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to) => AllowedTransitions[from].Contains(to);

    private void MoveTo(ApplicationStatus next)
    {
        if (!CanTransition(Status, next))
            throw new DomainException($"Başvuru {Status} durumundan {next} durumuna geçemez.");

        Status = next;
        Version = Guid.NewGuid();
    }

    // İncelemeyi üstlenen denetçi karar verir: sorumluluk belli olur, başkası araya giremez.
    private void EnsureReviewer(Guid inspectorId)
    {
        if (Status == ApplicationStatus.UnderReview && ReviewerId != inspectorId)
            throw new DomainException("Karar sadece incelemeyi üstlenen denetçi tarafından verilebilir.");
    }
}
