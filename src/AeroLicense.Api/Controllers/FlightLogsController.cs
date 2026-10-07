using AeroLicense.Api.Extensions;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.FlightLogs;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/flight-logs")]
[Produces("application/json")]
public sealed class FlightLogsController(IFlightLogService flightLogService) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotent-Replayed";

    /// <summary>
    /// Havayolu sisteminden uçuş kaydı alır. Idempotency-Key zorunludur: ağ hatasında istemci aynı anahtarla
    /// güvenle tekrar deneyebilir, ikinci kayıt oluşmaz ve ilk sonuç (200 + Idempotent-Replayed: true) döner.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Airline)]
    [EnableRateLimiting(RateLimitPolicies.Airline)]
    [ProducesResponseType<FlightLogDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<FlightLogDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<FlightLogDto>> Create(
        CreateFlightLogRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await flightLogService.CreateAsync(request, idempotencyKey, cancellationToken);
        if (!result.Replayed)
            return StatusCode(StatusCodes.Status201Created, result.FlightLog);

        Response.Headers[ReplayedHeader] = "true";
        return Ok(result.FlightLog);
    }

    /// <summary>
    /// Uçuş kayıtları. Havayolu sadece kendi gönderdiklerini, denetçi tümünü görür.
    /// suspicious=true: süre ≥ 12 saat veya uçuştan 30+ gün sonra bildirilmiş kayıtlar.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Airline},{Roles.Inspector}")]
    [ProducesResponseType<PagedResult<FlightLogListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<FlightLogListItemDto>>> List(
        [FromQuery] FlightLogQuery query, CancellationToken cancellationToken) =>
        Ok(await flightLogService.ListAsync(query, cancellationToken));
}
