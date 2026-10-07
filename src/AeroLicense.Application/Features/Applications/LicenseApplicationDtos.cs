using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Features.Applications;

// Başvuru sahibi kimliği gövdede yok: token'dan alınır.
public sealed record CreateLicenseApplicationRequest(LicenseType LicenseType);

public sealed record RejectLicenseApplicationRequest(string Reason);

public enum ApplicationSortField
{
    CreatedAt = 1,
    SubmittedAt = 2
}

public enum SortDirection
{
    Desc = 1,
    Asc = 2
}

public sealed class LicenseApplicationQuery : PageQuery
{
    public ApplicationStatus? Status { get; init; }

    // Serbest kolon adı değil enum: istemci sadece izin verilen alanlara göre sıralayabilir.
    public ApplicationSortField SortBy { get; init; } = ApplicationSortField.CreatedAt;
    public SortDirection SortDir { get; init; } = SortDirection.Desc;
}

public sealed record LicenseApplicationDto(
    Guid Id,
    Guid ApplicantId,
    string ApplicantName,
    LicenseType LicenseType,
    ApplicationStatus Status,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    Guid? TrainingRecordId,
    Guid? ReviewerId,
    string? ReviewerName,
    DateTime? ReviewStartedAtUtc,
    DateTime? DecidedAtUtc,
    string? RejectionReason,
    string? LicenseNumber,
    DateTime? LicenseExpiresAtUtc);
