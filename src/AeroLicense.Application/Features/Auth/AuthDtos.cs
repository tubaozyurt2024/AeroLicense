using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Role,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CurrentUserResponse(Guid UserId, UserRole Role, string FullName, Guid? OrganizationId, string? OrganizationName);
