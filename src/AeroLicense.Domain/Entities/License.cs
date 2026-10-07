using System.Security.Cryptography;
using AeroLicense.Domain.Common;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Domain.Entities;

/// <summary>Onaylanan başvurudan doğan lisans. Sadece LicenseApplication.Approve() tarafından oluşturulur.</summary>
public sealed class License : Entity
{
    // Prototip basitleştirmesi: tüm lisans türleri için tek geçerlilik süresi.
    public const int ValidityYears = 2;

    // Karışabilecek karakterler (0/O, 1/I) yok: numara telefonda okunurken hata yapılmasın.
    private const string NumberAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private License() { } // EF Core için

    internal License(Guid holderId, Guid applicationId, LicenseType type, DateTime issuedAtUtc)
    {
        HolderId = holderId;
        ApplicationId = applicationId;
        Type = type;
        // Saniyeye yuvarlanır: PostgreSQL mikrosaniye saklar, .NET 100 ns tutar. İmzalı belgedeki tarih ile
        // veritabanından okunan tarih bire bir aynı olmalı, yoksa doğrulama yanlışlıkla "değiştirilmiş" der.
        issuedAtUtc = new DateTime(issuedAtUtc.Ticks - issuedAtUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = issuedAtUtc.AddYears(ValidityYears);
        LicenseNumber = $"AL-{type}-{issuedAtUtc:yyyy}-{RandomNumberGenerator.GetString(NumberAlphabet, 6)}";
    }

    public string LicenseNumber { get; private set; } = null!;
    public Guid HolderId { get; private set; }
    public User Holder { get; private set; } = null!;
    public Guid ApplicationId { get; private set; }
    public LicenseType Type { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsValidAt(DateTime utc) => RevokedAtUtc is null && IssuedAtUtc <= utc && utc < ExpiresAtUtc;

    public LicenseStatus StatusAt(DateTime utc) => StatusOf(RevokedAtUtc, ExpiresAtUtc, utc);

    // Projeksiyonlarda (entity yüklenmeden) aynı kuralı kullanabilmek için statik hali.
    public static LicenseStatus StatusOf(DateTime? revokedAtUtc, DateTime expiresAtUtc, DateTime utc) =>
        revokedAtUtc is not null ? LicenseStatus.Revoked
        : utc >= expiresAtUtc ? LicenseStatus.Expired
        : LicenseStatus.Active;

    public void Revoke(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("İptal gerekçesi zorunludur.");
        if (RevokedAtUtc is not null) throw new DomainException("Lisans zaten iptal edilmiş.");
        RevokedAtUtc = nowUtc;
        RevocationReason = reason.Trim();
    }
}
