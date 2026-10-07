using AeroLicense.Application.Common.Pagination;
using FluentValidation;

namespace AeroLicense.Application.Features.Licenses;

public sealed class LicenseQueryValidator : PageQueryValidator<LicenseQuery>
{
    public LicenseQueryValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class RevokeLicenseRequestValidator : AbstractValidator<RevokeLicenseRequest>
{
    public RevokeLicenseRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1000);
}
