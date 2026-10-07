using AeroLicense.Domain.Common;

namespace AeroLicense.Domain.Entities;

/// <summary>
/// Değiştirilemez iz kaydı: kim, ne zaman, hangi kayıtta, hangi işlemi yaptı. Setter'lar private ve
/// güncelleme metodu yok; kayıt sadece eklenir (append-only).
/// </summary>
public sealed class AuditLog : Entity
{
    private AuditLog() { } // EF Core için

    public AuditLog(Guid? actorUserId, string action, string entityType, Guid entityId, DateTime occurredAtUtc,
        string? details = null)
    {
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OccurredAtUtc = occurredAtUtc;
        Details = details;
    }

    /// <summary>İşlemi yapan kullanıcı; sistem tarafından yapılan işlemlerde null.</summary>
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Serbest açıklama. Kişisel veri buraya maskelenmeden yazılmaz.</summary>
    public string? Details { get; private set; }

    /// <summary>
    /// İsteğin correlation ID'si: audit kaydından aynı isteğin log satırlarına geçmeyi sağlar. Servisler
    /// bunu bilmez; kaydetme sırasında altyapı (EF interceptor) doldurur.
    /// </summary>
    public string? CorrelationId { get; private set; }
}
