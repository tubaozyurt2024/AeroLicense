using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Training;
using AeroLicense.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AeroLicense.Api.Controllers;

[ApiController]
[Route(ApiRoutes.V1 + "/training-records")]
[Produces("application/json")]
public sealed class TrainingRecordsController(ITrainingService trainingService) : ControllerBase
{
    /// <summary>Eğitim kuruluşu, bir kişinin tamamladığı eğitimi ve sınav sonucunu kaydeder.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.TrainingOrg)]
    [ProducesResponseType<TrainingRecordDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingRecordDto>> Create(
        CreateTrainingRecordRequest request, CancellationToken cancellationToken)
    {
        var record = await trainingService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, record);
    }

    /// <summary>
    /// Eğitim kayıtlarını sayfalı listeler. Başvuru sahibi sadece kendi kayıtlarını, eğitim kuruluşu
    /// sadece kendi girdiği kayıtları, denetçi tüm kayıtları görür.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Applicant},{Roles.TrainingOrg},{Roles.Inspector}")]
    [ProducesResponseType<PagedResult<TrainingRecordDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TrainingRecordDto>>> List(
        [FromQuery] TrainingRecordQuery query, CancellationToken cancellationToken) =>
        Ok(await trainingService.ListAsync(query, cancellationToken));
}
