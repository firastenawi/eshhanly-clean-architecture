using Eshhanly.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Eshhanly.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps <see cref="Entity.CreatedAt"/> / <see cref="Entity.UpdatedAt"/> so no use case has to remember it.</summary>
internal sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified || entry.References.Any(r => r.TargetEntry?.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
