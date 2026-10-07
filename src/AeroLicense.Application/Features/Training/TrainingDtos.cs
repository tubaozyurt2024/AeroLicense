using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Features.Training;

// Eğitim kuruluşu kişiyi TC kimlik no ile bilir; sistem içi Guid'i bilmesi beklenmez.
public sealed record CreateTrainingRecordRequest(
    string ApplicantNationalId,
    LicenseType LicenseType,
    DateTime CompletedAtUtc,
    int ExamScore);

public sealed class TrainingRecordQuery : PageQuery
{
    public Guid? ApplicantId { get; init; }
    public bool PassedOnly { get; init; }
}

public sealed record TrainingRecordDto(
    Guid Id,
    Guid ApplicantId,
    string ApplicantName,
    string? ApplicantNationalIdMasked,
    string TrainingOrgName,
    LicenseType LicenseType,
    DateTime CompletedAtUtc,
    int ExamScore,
    bool IsPassed);
