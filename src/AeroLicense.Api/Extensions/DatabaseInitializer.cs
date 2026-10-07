using AeroLicense.Infrastructure.Persistence;
using AeroLicense.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Api.Extensions;

public static class DatabaseInitializer
{
    /// <summary>
    /// Prototip kolaylığı: açılışta migration + seed. Üretimde migration'lar CI/CD'de
    /// (idempotent SQL script veya migration bundle) onaylı bir adım olarak çalıştırılır.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var configuration = app.Configuration;

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        if (configuration.GetValue<bool>("Seed:Enabled"))
        {
            var demoPassword = configuration["Seed:DemoPassword"]
                ?? throw new InvalidOperationException("Seed:DemoPassword tanımlı değil (ortam değişkeni veya user-secrets).");
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(demoPassword);
        }
    }
}
