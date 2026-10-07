using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Audit;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/audit-logs")]
[Produces("application/json")]
[Authorize(Roles = Roles.Inspector)]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    /// <summary>
    /// İz kayıtları: kim, ne zaman, hangi kayıtta, hangi işlemi yaptı. Örn. bir başvurunun tüm geçmişi için
    /// ?entityType=LicenseApplication&amp;entityId=... Salt okunur; audit kaydını değiştiren endpoint yoktur.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] AuditLogQuery query, CancellationToken cancellationToken) =>
        Ok(await auditLogService.ListAsync(query, cancellationToken));
}
