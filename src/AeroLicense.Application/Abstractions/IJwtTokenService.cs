using AeroLicense.Domain.Entities;

namespace AeroLicense.Application.Abstractions;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user);

    TimeSpan RefreshTokenLifetime { get; }
}
