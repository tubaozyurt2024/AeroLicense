using AeroLicense.Application.Abstractions;

namespace AeroLicense.Api.Services;

/// <summary>CorrelationIdMiddleware, ID'yi TraceIdentifier'a yazar; burada oradan okunur.</summary>
public sealed class HttpCorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
