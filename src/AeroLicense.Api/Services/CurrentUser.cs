using System.Security.Claims;
using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Domain.Enums;
using AeroLicense.Infrastructure.Security;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AeroLicense.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("HTTP bağlamı dışında kullanıcı okunamaz.");

    public Guid UserId => Guid.Parse(Required(JwtRegisteredClaimNames.Sub));

    public UserRole Role => Enum.Parse<UserRole>(Required(JwtTokenService.RoleClaim));

    public Guid? OrganizationId =>
        Principal.FindFirstValue(JwtTokenService.OrganizationClaim) is { } value ? Guid.Parse(value) : null;

    private string Required(string claimType) =>
        Principal.FindFirstValue(claimType) ?? throw new UnauthorizedException("Token gerekli bilgiyi içermiyor.");
}
