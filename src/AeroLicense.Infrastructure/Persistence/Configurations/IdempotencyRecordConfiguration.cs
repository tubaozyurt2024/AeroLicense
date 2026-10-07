using AeroLicense.Application.Common.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.Property(r => r.Key).HasMaxLength(64).IsRequired();
        builder.Property(r => r.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();

        // Idempotency'nin asıl garantisi bu: eşzamanlı iki istekten sadece biri bu satırı yazabilir.
        builder.HasIndex(r => new { r.ClientId, r.Key }).IsUnique();
        // Saklama süresi dolan anahtarları temizleyecek bir iş için (üretimde ör. 24 saat).
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
