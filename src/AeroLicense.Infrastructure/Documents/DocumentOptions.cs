using System.ComponentModel.DataAnnotations;

namespace AeroLicense.Infrastructure.Documents;

public sealed class DocumentSigningOptions
{
    public const string SectionName = "DocumentSigning";

    /// <summary>
    /// appsettings'te YOK: DocumentSigning__Key ortam değişkeni veya user-secrets. JWT anahtarından ayrı:
    /// biri sızarsa diğeri etkilenmez, ayrı ayrı döndürülebilir. Üretimde HSM/Key Vault'ta tutulur.
    /// </summary>
    [Required, MinLength(32)] public string Key { get; init; } = string.Empty;

    [Required, MaxLength(32)] public string KeyId { get; init; } = "k1";
}

public sealed class VerificationOptions
{
    public const string SectionName = "Verification";

    /// <summary>QR'a gömülen herkese açık sayfa adresi; sonuna doğrulama kodu eklenir.</summary>
    [Required, Url] public string PublicBaseUrl { get; init; } = string.Empty;
}
