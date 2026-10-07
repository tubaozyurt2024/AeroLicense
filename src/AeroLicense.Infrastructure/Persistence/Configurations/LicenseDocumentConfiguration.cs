using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class LicenseDocumentConfiguration : IEntityTypeConfiguration<LicenseDocument>
{
    public void Configure(EntityTypeBuilder<LicenseDocument> builder)
    {
        builder.ToTable("license_documents");
        builder.Property(d => d.VerificationCode).HasMaxLength(LicenseDocument.CodeLength).IsRequired();
        builder.HasIndex(d => d.VerificationCode).IsUnique(); // doğrulama sorgusu bu index'le tek satır okur
        builder.Property(d => d.Content).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.Signature).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(d => d.KeyId).HasMaxLength(32).IsRequired();

        builder.HasOne(d => d.License).WithMany().HasForeignKey(d => d.LicenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => new { d.LicenseId, d.IssuedAtUtc });
    }
}
