using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.Audit;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken);
}

public sealed class AuditLogService(IAppDbContext db) : IAuditLogService
{
    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var logs = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.EntityType)) logs = logs.Where(a => a.EntityType == query.EntityType);
        if (query.EntityId is { } entityId) logs = logs.Where(a => a.EntityId == entityId);
        if (query.ActorUserId is { } actorId) logs = logs.Where(a => a.ActorUserId == actorId);
        if (!string.IsNullOrWhiteSpace(query.Action)) logs = logs.Where(a => a.Action == query.Action);
        if (query.FromUtc is { } from) logs = logs.Where(a => a.OccurredAtUtc >= from);
        if (query.ToUtc is { } to) logs = logs.Where(a => a.OccurredAtUtc <= to);

        var totalCount = await logs.CountAsync(cancellationToken);

        // FK olmadığı için (audit, kullanıcı silinse de kalır) LEFT JOIN: aktörü bulunamayan kayıt da listelenir.
        var rows = await (
                from a in logs
                join u in db.Users on a.ActorUserId equals (Guid?)u.Id into actors
                from u in actors.DefaultIfEmpty()
                orderby a.OccurredAtUtc descending, a.Id descending
                select new { Log = a, Email = u != null ? u.Email : null, Role = u != null ? (Domain.Enums.UserRole?)u.Role : null })
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AuditLogDto(r.Log.Id, r.Log.OccurredAtUtc, r.Log.ActorUserId,
            r.Email is null ? null : Masking.Email(r.Email), r.Role, r.Log.Action, r.Log.EntityType, r.Log.EntityId,
            r.Log.Details, r.Log.CorrelationId)).ToList();

        return new PagedResult<AuditLogDto>(items, query.Page, query.PageSize, totalCount);
    }
}
