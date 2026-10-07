using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AeroLicense.Api.Controllers;
using AeroLicense.Api.Filters;
using AeroLicense.Api.Middleware;
using AeroLicense.Api.Services;
using AeroLicense.Application.Abstractions;
using AeroLicense.Infrastructure.Persistence;
using AeroLicense.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace AeroLicense.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public const string CorsPolicy = "AllowedOrigins";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<ValidationFilter>())
            // Enum'lar JSON'da sayı değil isim olarak ("PPL"): okunabilir ve enum sırası değişince kırılmaz.
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Bozuk JSON'da iç tip adları/serializer mesajları istemciye sızmasın.
                options.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            {
                // Model binding hataları (okunamayan JSON, yanlış enum) da diğer hatalarla aynı formatta dönsün.
                var problem = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                    .CreateValidationProblemDetails(context.HttpContext, context.ModelState,
                        StatusCodes.Status400BadRequest, "Doğrulama hatası", detail: "İstek gövdesi okunamadı veya geçersiz.");
                return new BadRequestObjectResult(problem);
            });
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();

        // Her hata yanıtına correlationId eklenir: kullanıcı bu ID'yi bildirince ilgili log satırları bulunur.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services
            .AddJwtAuthentication()
            .AddRateLimiting(configuration)
            .AddCorsPolicy(configuration)
            .AddSwagger();
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Doğrulama parametreleri, token'ı üreten tarafla aynı JwtOptions'tan okunur (tek kaynak).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // "sub", "role" claim adları olduğu gibi kalsın
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.Value.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], // "alg" değiştirme saldırılarına karşı
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30), // varsayılan 5 dk, kısa ömürlü token'ı anlamsızlaştırır
                    NameClaimType = "sub",
                    RoleClaimType = JwtTokenService.RoleClaim
                };
            });

        // Varsayılan olarak güvenli: [AllowAnonymous] yazılmamış her endpoint giriş ister.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var airlinePermitPerMinute = configuration.GetValue("RateLimiting:AirlinePermitPerMinute", 60);
        var loginPermitPerMinute = configuration.GetValue("RateLimiting:LoginPermitPerMinute", 5);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // İstemci ne zaman tekrar deneyebileceğini bilsin (entegrasyonlar bu başlığa göre geri çekilir).
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                return ValueTask.CompletedTask;
            };

            // Login: IP başına dakikada 5 deneme (kaba kuvvet / credential stuffing'e karşı).
            options.AddPolicy(RateLimitPolicies.Login, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginPermitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Havayolu başına kota: bölüm anahtarı token'daki kuruluş kimliği. IP'ye göre olsaydı aynı NAT
            // arkasındaki havayolları birbirinin kotasını yerdi; bir havayolunun hatalı döngüsü diğerini etkilemez.
            options.AddPolicy(RateLimitPolicies.Airline, context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    context.User.FindFirst(JwtTokenService.OrganizationClaim)?.Value ?? "anonymous",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        // Token bucket: kısa patlamalara (gün sonu toplu gönderim) izin verir, ortalamayı sınırlar.
                        TokenLimit = airlinePermitPerMinute,
                        TokensPerPeriod = airlinePermitPerMinute,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Girişsiz doğrulama: kimlik olmadığı için IP başına. Kod tahmin edilemez olsa da toplu tarama
            // denemelerini yavaşlatır ve veritabanını korur.
            options.AddPolicy(RateLimitPolicies.Verify, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        // Joker (*) yok: sadece yapılandırmada açıkça izin verilen origin'ler, başlıklar ve metotlar.
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST")
            .WithHeaders("Authorization", "Content-Type", "Idempotency-Key", CorrelationIdMiddleware.HeaderName)
            // Tarayıcı JS'i, CORS'ta açıkça izin verilmeyen yanıt başlıklarını okuyamaz.
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, FlightLogsController.ReplayedHeader, "Retry-After")));
        return services;
    }

    private static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AeroLicense API",
                Version = "v1",
                Description = "Kurgusal bir Sivil Havacılık Otoritesi için lisans ve uçuş kaydı prototipi."
            });

            // Önyüz tipleri bu şemadan üretilir: C#'taki null olabilirlik şemaya birebir yansımalı.
            // Aksi halde her alan "?: T | null" olur ve TypeScript tarafında tip güvenliği kaybolur.
            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<RequiredNotNullablePropertiesSchemaFilter>();
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "POST /api/v1/auth/login'den alınan access token"
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
        return services;
    }
}
