using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/licenses")]
[Produces("application/json")]
public sealed class LicensesController(ILicenseService licenseService) : ControllerBase
{
    /// <summary>Lisansları listeler. Lisans sahibi kendi lisanslarını (doğrulama bağlantısıyla), denetçi tümünü görür.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.Inspector}")]
    [ProducesResponseType<PagedResult<LicenseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LicenseDto>>> List([FromQuery] LicenseQuery query, CancellationToken cancellationToken) =>
        Ok(await licenseService.ListAsync(query, cancellationToken));

    /// <summary>Belgenin doğrulama adresini içeren QR kodu (PNG).</summary>
    [HttpGet("{id:guid}/qr")]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.Inspector}")]
    [Produces("image/png")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> QrCode(Guid id, CancellationToken cancellationToken) =>
        File(await licenseService.GetQrCodePngAsync(id, cancellationToken), "image/png");

    /// <summary>Lisansı iptal eder. Belge doğrulaması bundan sonra "Revoked" döner.</summary>
    [HttpPost("{id:guid}/revoke")]
    [Authorize(Roles = Roles.Inspector)]
    [ProducesResponseType<LicenseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicenseDto>> Revoke(Guid id, RevokeLicenseRequest request, CancellationToken cancellationToken) =>
        Ok(await licenseService.RevokeAsync(id, request, cancellationToken));
}
