using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Bookings;

public sealed record BookingCreated(Guid BookingId, Guid StationId, Guid GuestId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingAwaitingApproval(Guid BookingId, Guid StationId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingConfirmed(Guid BookingId, Guid StationId, Guid GuestId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingCancelled(Guid BookingId, BookingParty CancelledBy, BookingStatus PreviousStatus, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingStarted(Guid BookingId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingCompleted(Guid BookingId, long FinalTotalMinor, string Currency, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingExpired(Guid BookingId, BookingStatus PreviousStatus, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record BookingMarkedNoShow(Guid BookingId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
