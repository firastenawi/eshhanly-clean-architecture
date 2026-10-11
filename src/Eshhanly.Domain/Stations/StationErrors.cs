using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

public static class StationErrors
{
    public static readonly Error NotFound = Error.NotFound("Station.NotFound", "Station was not found.");
    public static readonly Error InvalidCoordinates = Error.Validation("Station.InvalidCoordinates", "Latitude must be within ±90 and longitude within ±180.");
    public static readonly Error InvalidTimeZone = Error.Validation("Station.InvalidTimeZone", "Unknown time zone id.");
    public static readonly Error InvalidBookingLimits = Error.Validation("Station.InvalidBookingLimits", "Booking limits must satisfy 15 ≤ min ≤ max ≤ 1440 minutes and 0 ≤ buffer ≤ 120.");
    public static readonly Error ParkingOnlyConnectors = Error.Validation("Station.ParkingOnlyConnectors", "Parking-only listings cannot have charging connectors.");
    public static readonly Error ConnectorTypeRequired = Error.Validation("Station.ConnectorTypeRequired", "A charging connector must have a plug type.");
    public static readonly Error InvalidPower = Error.Validation("Station.InvalidPower", "Connector power must be between 1 and 400 kW.");
    public static readonly Error ConnectorNotFound = Error.NotFound("Station.ConnectorNotFound", "Connector was not found on this station.");
    public static readonly Error LastConnector = Error.Conflict("Station.LastConnector", "A station must keep at least one connector.");
    public static readonly Error InvalidPrice = Error.Validation("Station.InvalidPrice", "Price must be greater than zero.");
    public static readonly Error InvalidPricingWindow = Error.Validation("Station.InvalidPricingWindow", "Pricing rule needs at least one day and both times (start < end) or neither.");
    public static readonly Error PerKwhOnParking = Error.Validation("Station.PerKwhOnParking", "Per-kWh pricing is not possible on a parking-only listing.");
    public static readonly Error PricingRuleNotFound = Error.NotFound("Station.PricingRuleNotFound", "Pricing rule was not found.");
    public static readonly Error InvalidAvailabilityWindow = Error.Validation("Station.InvalidAvailabilityWindow", "Availability window start must be before its end.");
    public static readonly Error OverlappingAvailability = Error.Validation("Station.OverlappingAvailability", "Availability windows on the same day must not overlap.");
    public static readonly Error OverrideNotFound = Error.NotFound("Station.OverrideNotFound", "Availability override was not found.");
    public static readonly Error TooManyPhotos = Error.Validation("Station.TooManyPhotos", "A station can have at most 10 photos.");
    public static readonly Error PhotoNotFound = Error.NotFound("Station.PhotoNotFound", "Photo was not found.");
    public static readonly Error NotReadyForReview = Error.Validation("Station.NotReadyForReview", "A station needs at least one connector, one pricing rule and one availability window before review.");
    public static readonly Error Suspended = Error.Forbidden("Station.Suspended", "This station is suspended by the platform.");

    public static Error InvalidTransition(StationStatus from, string action) =>
        Error.Conflict("Station.InvalidTransition", $"Cannot {action} a station in status {from}.");
}
