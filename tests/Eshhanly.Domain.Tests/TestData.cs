using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;

namespace Eshhanly.Domain.Tests;

internal static class TestData
{
    /// <summary>A Sunday, 08:00 Damascus time (UTC+3).</summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 11, 5, 0, 0, TimeSpan.Zero);

    public static Station NewStation(ListingType type = ListingType.ChargingAndParking, BookingMode mode = BookingMode.Instant)
    {
        var station = Station.Create(
            hostId: Guid.CreateVersion7(),
            listingType: type,
            details: new StationDetails("شاحن المزة", "Home charger", "Ring the bell", mode),
            latitude: 33.5138,
            longitude: 36.2765,
            address: new Address("Mezzeh, Villas East", "Damascus")).Value;

        return station;
    }

    public static Station NewReadyStation(BookingMode mode = BookingMode.Instant)
    {
        var station = NewStation(mode: mode);
        station.AddConnector(ConnectorType.Type2, CurrentType.Ac, 7.4m);
        station.AddPricingRule(PricingModel.PerHour, 5_000);
        station.SetWeeklyAvailability(Enum.GetValues<DayOfWeek>().Select(d => (d, new TimeOnly(8, 0), new TimeOnly(22, 0))));
        return station;
    }

    public static TimeSlot Slot(int startHoursFromNow, int durationHours) =>
        TimeSlot.Create(Now.AddHours(startHoursFromNow), Now.AddHours(startHoursFromNow + durationHours)).Value;

    public static Booking NewBooking(int startHoursFromNow = 2)
    {
        var quote = new PriceQuote { Model = PricingModel.PerHour, Currency = "SYP", TotalMinor = 10_000 };
        return Booking.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), null,
            Slot(startHoursFromNow, 2), bufferMinutes: 15, quote, Now).Value;
    }
}
