namespace AeroLicense.Application.Common.Idempotency;

/// <summary>
/// Başarıyla işlenmiş bir isteğin anahtarı. İş kuralı değil teknik bir kayıt olduğu için Domain'de değil
/// Application'da. Oluşturduğu kayıtla aynı SaveChanges'te yazılır: ya ikisi birden kalıcı olur ya hiçbiri.
/// </summary>
public sealed class IdempotencyRecord
{
    private IdempotencyRecord() { } // EF Core için

    public IdempotencyRecord(Guid clientId, string key, string requestHash, Guid resourceId, DateTime createdAtUtc)
    {
        ClientId = clientId;
        Key = key;
        RequestHash = requestHash;
        ResourceId = resourceId;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }

    /// <summary>Anahtarın kapsamı (havayolu). İki havayolu aynı anahtarı kullansa da çakışmaz.</summary>
    public Guid ClientId { get; private set; }
    public string Key { get; private set; } = null!;

    /// <summary>İstek gövdesinin SHA-256 özeti: aynı anahtarla farklı içerik gönderilmesini yakalar.</summary>
    public string RequestHash { get; private set; } = null!;

    public Guid ResourceId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
