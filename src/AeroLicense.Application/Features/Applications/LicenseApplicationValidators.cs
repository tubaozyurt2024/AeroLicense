using AeroLicense.Application.Common.Pagination;
using FluentValidation;

namespace AeroLicense.Application.Features.Applications;

public sealed class CreateLicenseApplicationRequestValidator : AbstractValidator<CreateLicenseApplicationRequest>
{
    public CreateLicenseApplicationRequestValidator() => RuleFor(x => x.LicenseType).IsInEnum();
}

public sealed class RejectLicenseApplicationRequestValidator : AbstractValidator<RejectLicenseApplicationRequest>
{
    // "red" gibi anlamsız gerekçeleri engellemek için alt sınır; başvuru sahibine gösterildiği için üst sınır.
    public RejectLicenseApplicationRequestValidator() =>
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1000);
}

public sealed class LicenseApplicationQueryValidator : PageQueryValidator<LicenseApplicationQuery>
{
    public LicenseApplicationQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDir).IsInEnum();
    }
}
