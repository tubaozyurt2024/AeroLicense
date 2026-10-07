using AeroLicense.Api.Extensions;
using AeroLicense.Application.Features.Licenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/verify")]
[Produces("application/json")]
public sealed class VerificationController(IDocumentVerificationService verificationService) : ControllerBase
{
    /// <summary>
    /// Girişsiz belge doğrulama (QR'daki adres). Geçerli / süresi dolmuş / iptal / değiştirilmiş bilgisini ve
    /// sadece gereken minimum veriyi (maskeli ad) döner. Bilinmeyen kod 404.
    /// </summary>
    [HttpGet("{code}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Verify)]
    // Sonuç zamanla değişir (iptal, süre dolumu): ara katmanlar ve tarayıcı önbelleğe almasın.
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<VerificationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<VerificationResultDto>> Verify(string code, CancellationToken cancellationToken) =>
        Ok(await verificationService.VerifyAsync(code, cancellationToken));
}
