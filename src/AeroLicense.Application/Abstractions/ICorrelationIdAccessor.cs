namespace AeroLicense.Application.Abstractions;

/// <summary>Geçerli isteğin correlation ID'si; HTTP isteği dışında (ör. açılıştaki seed) null.</summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}
