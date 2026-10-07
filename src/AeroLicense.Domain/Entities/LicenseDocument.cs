using System.Security.Cryptography;
using AeroLicense.Domain.Common;

namespace AeroLicense.Domain.Entities;

/// <summary>
/// Lisansın doğrulanabilir belgesi: imzalanmış içerik özeti + herkese açık doğrulama kodu (QR'da).
/// İçerik JSON olarak aynen saklanır ve imza bu byte'lar üzerinden hesaplanır; doğrulamada tekrar
/// serileştirme yapılmadığı için alan sırası/format farkı yanlış "değiştirilmiş" sonucu üretmez.
/// </summary>
public sealed class LicenseDocument : Entity
{
    // Karışan karakterler (0/O, 1/I/L, U) yok: 30 karakter. 26 karakter × log2(30) ≈ 127 bit rastgelelik,
    // yani tahmin ederek geçerli bir kod bulmak pratikte imkânsız (UUID v4 ile aynı düzey).
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTVWXYZ23456789";
    public const int CodeLength = 26;

    private LicenseDocument() { } // EF Core için

    public LicenseDocument(Guid licenseId, string content, string signature, string keyId, DateTime issuedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Belge içeriği boş olamaz.");
        if (string.IsNullOrWhiteSpace(signature)) throw new DomainException("Belge imzasız olamaz.");

        LicenseId = licenseId;
        Content = content;
        Signature = signature;
        KeyId = keyId;
        IssuedAtUtc = issuedAtUtc;
        VerificationCode = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
    }

    public Guid LicenseId { get; private set; }
    public License License { get; private set; } = null!;

    /// <summary>Tahmin edilemez, herkese açık kod. Kimlik değil; sadece bu belgeye işaret eder.</summary>
    public string VerificationCode { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    /// <summary>HMAC-SHA256(Content), hex.</summary>
    public string Signature { get; private set; } = null!;

    /// <summary>Hangi anahtarla imzalandığı: anahtar döndürüldüğünde eski belgeler eski anahtarla doğrulanır.</summary>
    public string KeyId { get; private set; } = null!;

    public DateTime IssuedAtUtc { get; private set; }

    public static bool IsWellFormedCode(string? code) =>
        code is { Length: CodeLength } && code.All(CodeAlphabet.Contains);
}
