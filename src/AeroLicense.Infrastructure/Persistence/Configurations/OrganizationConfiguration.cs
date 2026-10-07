using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();
        builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(32);
    }
}
