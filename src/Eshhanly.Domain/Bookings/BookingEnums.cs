namespace Eshhanly.Domain.Bookings;

/// <summary>
/// Values are persisted and referenced by the DB exclusion constraint (statuses 0-3 block the slot) — never renumber.
/// </summary>
public enum BookingStatus
{
    /// <summary>Created; waiting for the payment hold.</summary>
    Pending = 0,
    /// <summary>Payment held; waiting for the host (RequiresApproval stations only).</summary>
    AwaitingHostApproval = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
    Expired = 7,
}

public enum BookingParty
{
    Guest = 0,
    Host = 1,
    Platform = 2,
}
