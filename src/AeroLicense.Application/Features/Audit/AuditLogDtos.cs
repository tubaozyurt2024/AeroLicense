using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Enums;
using FluentValidation;

namespace AeroLicense.Application.Features.Audit;

public sealed class AuditLogQuery : PageQuery
{
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? Action { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

/// <summary>Denetçiye bile e-posta maskeli: kişiyi ayırt etmek için UserId yeter (veri minimizasyonu).</summary>
public sealed record AuditLogDto(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorEmailMasked,
    UserRole? ActorRole,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Details,
    string? CorrelationId);

public sealed class AuditLogQueryValidator : PageQueryValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.EntityType).MaximumLength(64);
        RuleFor(x => x.Action).MaximumLength(64);
        RuleFor(x => x.ToUtc).GreaterThanOrEqualTo(x => x.FromUtc).When(x => x.FromUtc is not null && x.ToUtc is not null)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");
    }
}
