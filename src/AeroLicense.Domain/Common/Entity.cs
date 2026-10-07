namespace AeroLicense.Domain.Common;

public abstract class Entity
{
    // Guid v7 zaman sıralıdır: rastgele Guid'e göre B-tree index'te daha az sayfa bölünmesi olur.
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
