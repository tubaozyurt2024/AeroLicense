using AeroLicense.Application.Abstractions;
using AeroLicense.Infrastructure.Documents;
using AeroLicense.Infrastructure.Persistence;
using AeroLicense.Infrastructure.Persistence.Interceptors;
using AeroLicense.Infrastructure.Persistence.Seed;
using AeroLicense.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppPasswordHasher = AeroLicense.Application.Abstractions.IPasswordHasher;

namespace AeroLicense.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStrings.Normalize(configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default tanımlı değil (ortam değişkeni veya user-secrets)."));

        services.AddScoped<AuditLogInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .AddInterceptors(sp.GetRequiredService<AuditLogInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Eksik/zayıf JWT anahtarıyla uygulama hiç ayağa kalkmasın (fail fast).
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DocumentSigningOptions>()
            .Bind(configuration.GetSection(DocumentSigningOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<VerificationOptions>()
            .Bind(configuration.GetSection(VerificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDocumentSigner, HmacDocumentSigner>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddSingleton<IVerificationLinks, VerificationLinks>();
        services.AddSingleton<AppPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
