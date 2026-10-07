using AeroLicense.Api.Extensions;
using AeroLicense.Api.Middleware;
using AeroLicense.Application;
using AeroLicense.Infrastructure;
using Serilog;
using Serilog.Formatting.Compact;

// Uygulama ayağa kalkarken oluşan hatalar da loglansın diye önce basit bir "bootstrap" logger.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) =>
    {
        logger.ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();

        // Geliştirmede okunabilir metin, diğer ortamlarda log toplayıcıların (ELK, Loki) işleyeceği JSON.
        if (builder.Environment.IsDevelopment())
            logger.WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}");
        else
            logger.WriteTo.Console(new RenderedCompactJsonFormatter());
    });

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi(builder.Configuration);

    var app = builder.Build();

    // Middleware sırası önemli:
    // 1) Correlation ID en dışta: sonraki her log satırı bu ID'yi taşır. Güvenlik başlıkları hata yanıtlarına da eklenir.
    // 2) İstek logu hata yakalayıcının dışında: istemciye giden GERÇEK durum kodunu (400, 422...) loglar.
    // 3) Hata yakalayıcı, alttaki tüm katmanların istisnalarını ProblemDetails'e çevirir.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages(); // gövdesiz 401/403/404/429 yanıtları da ProblemDetails olsun

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Doğrulama sayfası (wwwroot/verify.html): API ile aynı origin'den sunulur, CORS gerekmez.
    app.UseStaticFiles();

    app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter(); // kimlik doğrulamadan sonra: havayolu bazlı limit token'daki claim'i kullanacak

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();
    // QR'daki insan-okur adres: /dogrula/{kod} → sayfa, sayfa da /api/v1/verify/{kod}'u çağırır.
    app.MapFallbackToFile("/dogrula/{code}", "verify.html").AllowAnonymous();

    await app.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException) // dotnet ef araçları host'u bu istisnayla durdurur
{
    Log.Fatal(ex, "Uygulama başlatılamadı");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Integration testlerde WebApplicationFactory<Program> için görünür olsun.
public partial class Program;
