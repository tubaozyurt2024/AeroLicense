using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class TrainingRecordConfiguration : IEntityTypeConfiguration<TrainingRecord>
{
    public void Configure(EntityTypeBuilder<TrainingRecord> builder)
    {
        // Domain kuralı veritabanında da korunur (derinlemesine savunma): başka bir yoldan da yanlış puan yazılamaz.
        builder.ToTable("training_records", t =>
            t.HasCheckConstraint("ck_training_records_exam_score", "\"ExamScore\" BETWEEN 0 AND 100"));

        builder.Property(r => r.LicenseType).HasConversion<string>().HasMaxLength(16);

        builder.HasOne(r => r.Applicant).WithMany().HasForeignKey(r => r.ApplicantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.TrainingOrg).WithMany().HasForeignKey(r => r.TrainingOrgId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Başvuru kontrolü "bu kişinin bu türde başarılı eğitimi var mı?" diye sorar: index o sorguya göre.
        builder.HasIndex(r => new { r.ApplicantId, r.LicenseType });
        builder.HasIndex(r => r.TrainingOrgId);
    }
}
