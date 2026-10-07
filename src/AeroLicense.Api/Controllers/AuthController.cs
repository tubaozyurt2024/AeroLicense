using AeroLicense.Api.Extensions;
using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>E-posta ve parola ile kısa ömürlü JWT access token alır.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Refresh token ile yeni access + refresh token alır (rotation). Kullanılmış bir refresh token tekrar
    /// gönderilirse çalındığı varsayılır ve o oturumun tüm belirteçleri iptal edilir.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.RefreshAsync(request, cancellationToken));

    /// <summary>Oturumu kapatır: bu refresh token'ın ailesi iptal edilir. Her durumda 204 döner.</summary>
    [HttpPost("logout")]
    [AllowAnonymous] // access token süresi dolmuş olsa da çıkış yapılabilmeli
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Oturumdaki kullanıcı: kimlik, rol, ad ve kuruluş (önyüz menü ve başlık için).</summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetCurrentUserAsync(currentUser.UserId, cancellationToken));
}
