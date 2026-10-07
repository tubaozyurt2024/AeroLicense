using AeroLicense.Application.Common.Idempotency;
using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Abstractions;

/// <summary>
/// Uygulama katmanının veriye eriştiği tek kapı. DbContext zaten Unit of Work, DbSet de repository
/// olduğu için ayrıca generic repository yazmıyoruz; testte SQLite in-memory ile gerçek sorgu çalışır.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<TrainingRecord> TrainingRecords { get; }
    DbSet<LicenseApplication> LicenseApplications { get; }
    DbSet<License> Licenses { get; }
    DbSet<LicenseDocument> LicenseDocuments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<FlightLog> FlightLogs { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
