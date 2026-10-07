using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.Property(a => a.Action).HasMaxLength(64).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);

        // Bilerek FK yok: audit kaydı, ilgili kayıt/kullanıcı silinse bile kalmalı.
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.OccurredAtUtc);
    }
}
