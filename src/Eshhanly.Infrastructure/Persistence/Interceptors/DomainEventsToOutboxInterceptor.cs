using System.Text.Json;
using Eshhanly.Domain.Common;
using Eshhanly.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Eshhanly.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Moves raised domain events into <see cref="OutboxMessage"/> rows inside the same SaveChanges/transaction,
/// so an event is persisted if and only if the state change that produced it is.
/// </summary>
internal sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enqueue(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enqueue(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Enqueue(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count != 0)
            .ToList();

        var messages = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Type = domainEvent.GetType().FullName!,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredAt = domainEvent.OccurredAt,
            })
            .ToList();

        aggregates.ForEach(a => a.ClearDomainEvents());
        context.Set<OutboxMessage>().AddRange(messages);
    }
}
