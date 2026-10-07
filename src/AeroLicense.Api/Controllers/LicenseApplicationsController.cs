using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Applications;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AeroLicense.Api.Controllers;

/// <summary>
/// Durum geçişleri ayrı "eylem" endpoint'leri (submit, approve...) olarak modellenir; PATCH ile status alanını
/// serbestçe yazdırmak yerine her geçişin kendi yetkisi ve kuralı olur.
/// </summary>
[ApiController]
[Route(ApiRoutes.V1 + "/license-applications")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class LicenseApplicationsController(ILicenseApplicationService service) : ControllerBase
{
    /// <summary>Başvuru sahibi taslak (Draft) bir lisans başvurusu oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Applicant)]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LicenseApplicationDto>> Create(
        CreateLicenseApplicationRequest request, CancellationToken cancellationToken)
    {
        var application = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = application.Id }, application);
    }

    /// <summary>Başvuruları sayfalı listeler. Başvuru sahibi kendi başvurularını, denetçi tümünü görür.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.Inspector}")]
    [ProducesResponseType<PagedResult<LicenseApplicationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LicenseApplicationDto>>> List(
        [FromQuery] LicenseApplicationQuery query, CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.Inspector}")]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LicenseApplicationDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    /// <summary>Draft → Submitted. İlgili lisans türünde başarılı eğitim kaydı şarttır.</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Applicant)]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicenseApplicationDto>> Submit(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.SubmitAsync(id, cancellationToken));

    /// <summary>Submitted → UnderReview. İncelemeyi başlatan denetçi başvurunun sorumlusu olur.</summary>
    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = Roles.Inspector)]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicenseApplicationDto>> StartReview(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.StartReviewAsync(id, cancellationToken));

    /// <summary>UnderReview → Approved. Lisans aynı işlemde oluşturulur.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Inspector)]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicenseApplicationDto>> Approve(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.ApproveAsync(id, cancellationToken));

    /// <summary>UnderReview → Rejected. Gerekçe zorunludur.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Inspector)]
    [ProducesResponseType<LicenseApplicationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicenseApplicationDto>> Reject(
        Guid id, RejectLicenseApplicationRequest request, CancellationToken cancellationToken) =>
        Ok(await service.RejectAsync(id, request, cancellationToken));
}
