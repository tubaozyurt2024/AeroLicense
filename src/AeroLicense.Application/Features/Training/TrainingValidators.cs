using AeroLicense.Application.Common.Pagination;
using FluentValidation;

namespace AeroLicense.Application.Features.Training;

public sealed class CreateTrainingRecordRequestValidator : AbstractValidator<CreateTrainingRecordRequest>
{
    public CreateTrainingRecordRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.ApplicantNationalId)
            .NotEmpty()
            .Matches("^[1-9][0-9]{10}$").WithMessage("TC kimlik no 11 haneli olmalı ve 0 ile başlamamalıdır.");
        RuleFor(x => x.LicenseType).IsInEnum();
        RuleFor(x => x.ExamScore).InclusiveBetween(0, 100);
        RuleFor(x => x.CompletedAtUtc)
            .NotEmpty()
            .Must(date => date <= timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("Tamamlanma tarihi gelecekte olamaz.");
    }
}

public sealed class TrainingRecordQueryValidator : PageQueryValidator<TrainingRecordQuery>;
