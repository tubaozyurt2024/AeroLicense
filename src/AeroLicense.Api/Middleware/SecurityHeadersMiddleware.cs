namespace AeroLicense.Api.Middleware;

/// <summary>
/// Tarayıcıya yönelik güvenlik başlıkları. API yanıtları JSON olsa da doğrulama sayfası HTML; başlıklar
/// her yanıtta aynı olsun diye tek yerde eklenir.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    // Sadece kendi origin'imiz + SRI ile sabitlenmiş CDN script'leri. Inline script yok (XSS'e karşı asıl koruma).
    private const string ContentSecurityPolicy =
        "default-src 'none'; script-src 'self' https://cdnjs.cloudflare.com; style-src 'self'; " +
        "connect-src 'self'; img-src 'self' data:; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";   // MIME tahmini yok: JSON'u script diye çalıştırtamazlar
        headers.XFrameOptions = "DENY";            // clickjacking: sayfa iframe'e gömülemez
        headers["Referrer-Policy"] = "no-referrer"; // doğrulama kodu başka sitelere Referer ile sızmaz

        // Swagger UI inline script kullanır; CSP sadece geliştirme aracı dışındaki yanıtlara uygulanır.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
            headers.ContentSecurityPolicy = ContentSecurityPolicy;

        return next(context);
    }
}
