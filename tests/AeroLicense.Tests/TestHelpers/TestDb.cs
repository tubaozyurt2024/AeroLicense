using AeroLicense.Application.Abstractions;
using AeroLicense.Infrastructure.Persistence;
using AeroLicense.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Tests.TestHelpers;

/// <summary>
/// SQLite in-memory: EF InMemory sağlayıcısından farkı gerçek bir SQL motoru olması; unique index,
/// foreign key gibi kısıtları gerçekten uygular. Bağlantı açık kaldığı sürece veritabanı yaşar.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDb()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public const string CorrelationId = "test-correlation-id";

    // Üretimdeki interceptor testte de takılı: audit davranışı gerçek yapılandırmayla test edilir.
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditLogInterceptor(new FixedCorrelationId()))
            .Options);

    private sealed class FixedCorrelationId : ICorrelationIdAccessor
    {
        public string? CorrelationId => TestDb.CorrelationId;
    }

    public void Dispose() => _connection.Dispose();
}
