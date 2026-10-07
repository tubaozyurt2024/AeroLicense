using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common.Idempotency;
using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
    public DbSet<LicenseApplication> LicenseApplications => Set<LicenseApplication>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseDocument> LicenseDocuments => Set<LicenseDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FlightLog> FlightLogs => Set<FlightLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Her entity'nin eşlemesi kendi IEntityTypeConfiguration sınıfında (Configurations/).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
