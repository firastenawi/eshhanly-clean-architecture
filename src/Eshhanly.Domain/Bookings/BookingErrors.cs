using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Bookings;

public static class BookingErrors
{
    public static readonly Error NotFound = Error.NotFound("Booking.NotFound", "Booking was not found.");
    public static readonly Error SlotTaken = Error.Conflict("Booking.SlotTaken", "This time slot is no longer available.");
    public static readonly Error StationNotBookable = Error.Conflict("Booking.StationNotBookable", "This station is not accepting bookings.");
    public static readonly Error StationClosed = Error.Validation("Booking.StationClosed", "The station is not open for the whole requested slot.");
    public static readonly Error InvalidDuration = Error.Validation("Booking.InvalidDuration", "Booking duration is outside the station's allowed limits.");
    public static readonly Error StartInPast = Error.Validation("Booking.StartInPast", "Booking must start in the future.");
    public static readonly Error OwnStation = Error.Validation("Booking.OwnStation", "Hosts cannot book their own station.");
    public static readonly Error IncompatibleVehicle = Error.Validation("Booking.IncompatibleVehicle", "The selected vehicle does not support this connector.");
    public static readonly Error VehicleRequired = Error.Validation("Booking.VehicleRequired", "A vehicle is required to book a charging connector.");
    public static readonly Error TooEarlyToStart = Error.Conflict("Booking.TooEarlyToStart", "The session can start at most 15 minutes before the booked time.");
    public static readonly Error NotExpiredYet = Error.Conflict("Booking.NotExpiredYet", "The booking deadline has not passed yet.");
    public static readonly Error GracePeriodNotOver = Error.Conflict("Booking.GracePeriodNotOver", "The no-show grace period has not passed yet.");

    public static Error InvalidTransition(BookingStatus from, string action) =>
        Error.Conflict("Booking.InvalidTransition", $"Cannot {action} a booking in status {from}.");
}
