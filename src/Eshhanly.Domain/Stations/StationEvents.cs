using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

public sealed record StationSubmittedForReview(Guid StationId, Guid HostId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record StationApproved(Guid StationId, Guid HostId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record StationRejected(Guid StationId, Guid HostId, string Reason, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record StationSuspended(Guid StationId, Guid HostId, string Reason, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
