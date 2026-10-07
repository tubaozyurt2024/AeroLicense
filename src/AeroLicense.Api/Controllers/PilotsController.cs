using AeroLicense.Application.Features.FlightLogs;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/pilots")]
[Produces("application/json")]
public sealed class PilotsController(IFlightLogService flightLogService) : ControllerBase
{
    /// <summary>Pilotun toplam, son 30 gün ve son 90 gün uçuş saatleri. Pilot sadece kendi özetini görür.</summary>
    [HttpGet("{id:guid}/summary")]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.Inspector}")]
    [ProducesResponseType<PilotSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PilotSummaryDto>> Summary(Guid id, CancellationToken cancellationToken) =>
        Ok(await flightLogService.GetPilotSummaryAsync(id, cancellationToken));
}
