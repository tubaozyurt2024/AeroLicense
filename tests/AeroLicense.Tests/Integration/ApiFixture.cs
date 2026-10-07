using System.Net.Http.Headers;
using System.Net.Http.Json;
using AeroLicense.Application.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AeroLicense.Tests.Integration;

/// <summary>
/// Gerçek API + gerçek PostgreSQL. SQLite'ın yakalayamadıkları burada test edilir: migration'lar,
/// kısmi unique index, trigger, eşzamanlılık, middleware zinciri (auth, ProblemDetails, rate limit).
///
/// Veritabanı kaynağı:
///  - AEROLICENSE_IT_POSTGRES tanımlıysa (ör. "Host=localhost;Port=5439;Username=postgres") o sunucuda
///    rastgele adlı geçici bir veritabanı açılır, test sonunda silinir.
///  - Değilse Testcontainers ile postgres:16-alpine konteyneri başlatılır (CI'da standart yol).
///  - İkisi de yoksa (Docker erişilemiyor) testler başarısız değil "atlandı" olarak raporlanır.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string DemoPassword = "Integration-Test-1";

    private PostgreSqlContainer? _container;
    private string? _serverConnectionString;
    private string? _databaseName;

    public WebApplicationFactory<Program>? Factory { get; private set; }
    public string? SkipReason { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            ConnectionString = await CreateDatabaseAsync();
        }
        catch (Exception ex)
        {
            SkipReason = $"PostgreSQL bulunamadı (Docker erişilemiyor ve AEROLICENSE_IT_POSTGRES tanımlı değil): {ex.Message}";
            return;
        }

        // Ortam değişkenleri en yüksek öncelikli yapılandırma kaynağı ve Program.cs daha host kurulmadan
        // okur; appsettings ve user-secrets'ı (geliştirici veritabanı!) kesin olarak ezer.
        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing", // Development değil: user-secrets yüklenmez
            ["ConnectionStrings__Default"] = ConnectionString,
            ["Jwt__SigningKey"] = new string('j', 48),
            ["DocumentSigning__Key"] = new string('d', 48),
            ["Database__MigrateOnStartup"] = "true",
            ["Seed__Enabled"] = "true",
            ["Seed__DemoPassword"] = DemoPassword,
            ["RateLimiting__LoginPermitPerMinute"] = "1000"
        };
        foreach (var (key, value) in settings)
            Environment.SetEnvironmentVariable(key, value);

        Factory = new WebApplicationFactory<Program>();
        Factory.CreateClient().Dispose(); // host'u başlat: migration + seed burada çalışır
    }

    public HttpClient Anonymous() => Factory!.CreateClient();

    public async Task<HttpClient> LoginAsync(string user)
    {
        var client = Factory!.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest($"{user}@aerolicense.test", DemoPassword));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    private async Task<string> CreateDatabaseAsync()
    {
        _serverConnectionString = Environment.GetEnvironmentVariable("AEROLICENSE_IT_POSTGRES");
        if (string.IsNullOrWhiteSpace(_serverConnectionString))
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync();
            return _container.GetConnectionString();
        }

        _databaseName = $"aerolicense_it_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = _databaseName }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();

        if (_databaseName is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(_serverConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Integration";
}

public static class Json
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
