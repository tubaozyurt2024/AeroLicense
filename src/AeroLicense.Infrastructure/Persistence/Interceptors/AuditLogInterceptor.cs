using AeroLicense.Application.Abstractions;
using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AeroLicense.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Audit kayıtları için iki kesişen sorumluluk, servisler bunları bilmeden:
/// 1) Yeni kayıtlara isteğin correlation ID'sini yazar.
/// 2) Mevcut bir audit kaydının güncellenmesini/silinmesini reddeder (append-only). Veritabanında aynı kural
///    bir trigger ile de korunur; bu katman hatayı daha erken ve anlaşılır biçimde verir.
/// </summary>
public sealed class AuditLogInterceptor(ICorrelationIdAccessor correlationIdAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Audit kayıtları değiştirilemez veya silinemez.");

            if (entry.State == EntityState.Added)
                entry.Property(a => a.CorrelationId).CurrentValue ??= correlationIdAccessor.CorrelationId;
        }
    }
}
