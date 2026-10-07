namespace AeroLicense.Domain.Common;

/// <summary>İş kuralı ihlali. API katmanında 422 Unprocessable Entity'ye çevrilir.</summary>
public sealed class DomainException(string message) : Exception(message);
