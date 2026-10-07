using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("licenses");
        builder.Property(l => l.LicenseNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(l => l.LicenseNumber).IsUnique();
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(l => l.RevocationReason).HasMaxLength(1000);

        builder.HasOne(l => l.Holder).WithMany().HasForeignKey(l => l.HolderId).OnDelete(DeleteBehavior.Restrict);

        // Bir başvurudan en fazla bir lisans doğar.
        builder.HasOne<LicenseApplication>().WithOne(a => a.License)
            .HasForeignKey<License>(l => l.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uçuş kaydı ve başvuru kontrolleri "bu kişinin geçerli lisansı var mı?" diye sorar.
        builder.HasIndex(l => new { l.HolderId, l.ExpiresAtUtc });
    }
}
