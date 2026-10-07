using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class LicenseApplicationConfiguration : IEntityTypeConfiguration<LicenseApplication>
{
    public void Configure(EntityTypeBuilder<LicenseApplication> builder)
    {
        builder.ToTable("license_applications");
        builder.Property(a => a.LicenseType).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasOne(a => a.Applicant).WithMany().HasForeignKey(a => a.ApplicantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Reviewer).WithMany().HasForeignKey(a => a.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TrainingRecord>().WithMany().HasForeignKey(a => a.TrainingRecordId).OnDelete(DeleteBehavior.Restrict);

        // Servisteki "açık başvuru var mı?" kontrolü iki eşzamanlı istekte ikisini de geçirebilir; kısmi
        // unique index bu yarışı veritabanı seviyesinde kapatır. Sonuçlanan başvurular kapsam dışında.
        builder.HasIndex(a => new { a.ApplicantId, a.LicenseType })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Draft', 'Submitted', 'UnderReview')")
            .HasDatabaseName("ux_license_applications_one_open_per_type");

        // Denetçinin iş kuyruğu: "Submitted olanları tarihe göre getir".
        builder.HasIndex(a => new { a.Status, a.CreatedAtUtc });
    }
}
