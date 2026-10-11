namespace Eshhanly.Domain.Common;

/// <summary>Something meaningful that happened inside an aggregate. Dispatched via the outbox after SaveChanges.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;
