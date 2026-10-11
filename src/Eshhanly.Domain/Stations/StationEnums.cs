namespace Eshhanly.Domain.Stations;

public enum ListingType
{
    ChargingAndParking = 0,
    ParkingOnly = 1,
}

public enum StationStatus
{
    Draft = 0,
    PendingReview = 1,
    Active = 2,
    Paused = 3,
    Suspended = 4,
}

public enum BookingMode
{
    Instant = 0,
    RequiresApproval = 1,
}

public enum CancellationPolicy
{
    /// <summary>Full refund up to 1 hour before start.</summary>
    Flexible = 0,
    /// <summary>Full refund up to 24 hours before start, 50% after.</summary>
    Moderate = 1,
    /// <summary>50% refund up to 24 hours before start, nothing after.</summary>
    Strict = 2,
}

public enum ConnectorStatus
{
    Available = 0,
    Occupied = 1,
    Faulted = 2,
    Offline = 3,
}

public enum PricingModel
{
    PerHour = 0,
    PerKwh = 1,
    PerSession = 2,
}

public enum AvailabilityOverrideKind
{
    Blocked = 0,
    ExtraOpen = 1,
}

/// <summary>Bitmask of week days (System.DayOfWeek is not a flags enum).</summary>
[Flags]
public enum WeekDays
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday,
}

public static class WeekDaysExtensions
{
    public static bool Includes(this WeekDays days, DayOfWeek day) => days.HasFlag((WeekDays)(1 << (int)day));
}
