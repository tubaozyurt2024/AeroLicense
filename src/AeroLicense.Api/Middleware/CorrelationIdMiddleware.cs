using Serilog.Context;

namespace AeroLicense.Api.Middleware;

/// <summary>
/// Her isteğe bir Correlation ID atar (gelen başlık geçerliyse onu kullanır), yanıt başlığına yazar ve
/// Serilog LogContext'e ekler. Böylece bir isteğin tüm log satırları, hatta havayolunun kendi
/// logları bile, tek bir ID ile ilişkilendirilebilir.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        // Dışarıdan gelen değer loglara yazılacağı için doğrulanır (log injection'a karşı).
        if (!IsValid(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        // TraceIdentifier ProblemDetails'teki traceId alanına da yansır: hata yanıtı ↔ log eşleşir.
        context.TraceIdentifier = correlationId!;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= 64
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
