using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Features.Licenses;

public sealed class LicenseQuery : PageQuery
{
    public LicenseStatus? Status { get; init; }
}

public sealed record RevokeLicenseRequest(string Reason);

public sealed record LicenseDto(
    Guid Id,
    string LicenseNumber,
    Guid HolderId,
    string HolderName,
    LicenseType Type,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    LicenseStatus Status,
    string? VerificationCode,
    string? VerificationUrl);

public enum VerificationStatus
{
    Valid = 1,
    Expired = 2,
    Revoked = 3,
    /// <summary>İmza tutmuyor veya içerik lisans kaydıyla uyuşmuyor: belge verisi değiştirilmiş.</summary>
    Tampered = 4
}

/// <summary>
/// Girişsiz doğrulama yanıtı: sadece "bu belge gerçek ve geçerli mi?" sorusuna yetecek kadar bilgi.
/// Ad maskeli; kimlik no, e-posta, uçuş bilgisi yok. Tampered durumunda içerik alanları boş döner.
/// </summary>
public sealed record VerificationResultDto(
    VerificationStatus Status,
    string? LicenseNumber,
    LicenseType? LicenseType,
    string? HolderNameMasked,
    DateTime? ExpiresAtUtc,
    DateTime CheckedAtUtc);
