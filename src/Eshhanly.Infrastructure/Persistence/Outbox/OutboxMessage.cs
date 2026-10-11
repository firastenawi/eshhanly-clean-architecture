namespace Eshhanly.Infrastructure.Persistence.Outbox;

/// <summary>
/// Domain event serialized in the same transaction as the aggregate change (Transactional Outbox).
/// A background processor (Step 4) publishes pending rows and marks them processed.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}
