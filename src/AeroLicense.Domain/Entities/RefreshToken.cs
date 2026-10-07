using AeroLicense.Domain.Common;

namespace AeroLicense.Domain.Entities;

/// <summary>
/// Uzun ömürlü, tek kullanımlık yenileme belirteci. Ham değer asla saklanmaz, sadece SHA-256 özeti.
/// Aynı girişten türeyen tüm belirteçler bir "aile" (FamilyId) oluşturur: kullanılmış bir belirteç tekrar
/// gelirse çalındığı varsayılır ve aile tümüyle iptal edilir (reuse detection).
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken() { } // EF Core için

    public RefreshToken(Guid userId, string tokenHash, Guid familyId, DateTime nowUtc, TimeSpan lifetime)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = nowUtc;
        ExpiresAtUtc = nowUtc.Add(lifetime);
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public Guid FamilyId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Concurrency token: aynı belirteçle eşzamanlı iki yenilemeden sadece biri kazanır.</summary>
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActiveAt(DateTime utc) => RevokedAtUtc is null && utc < ExpiresAtUtc;

    public RefreshToken Rotate(string newTokenHash, DateTime nowUtc, TimeSpan lifetime)
    {
        if (!IsActiveAt(nowUtc)) throw new DomainException("Yenileme belirteci aktif değil.");

        var next = new RefreshToken(UserId, newTokenHash, FamilyId, nowUtc, lifetime);
        RevokedAtUtc = nowUtc;
        ReplacedByTokenId = next.Id;
        return next;
    }

    public void Revoke(DateTime nowUtc) => RevokedAtUtc ??= nowUtc;
}
