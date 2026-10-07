using FluentValidation;

namespace AeroLicense.Application.Common.Pagination;

public abstract class PageQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Tüm sayfalı sorgular bu kuralları miras alır: üst sınır, tek istekte tüm tabloyu çekmeyi engeller.</summary>
public abstract class PageQueryValidator<T> : AbstractValidator<T> where T : PageQuery
{
    protected PageQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageQuery.MaxPageSize);
    }
}
