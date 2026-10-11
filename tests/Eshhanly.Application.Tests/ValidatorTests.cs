using Eshhanly.Application.Bookings;
using Eshhanly.Application.Stations;
using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Stations;

namespace Eshhanly.Application.Tests;

public class ValidatorTests
{
    private static RegisterStationCommand ValidStation() => new(
        ListingType.ChargingAndParking, "شاحن المزة", null, null, 33.51, 36.27, "Mezzeh", "Damascus", BookingMode.Instant,
        [new ConnectorInput(ConnectorType.Type2, CurrentType.Ac, 7.4m)],
        [new PricingInput(PricingModel.PerHour, 5_000)],
        [new AvailabilityInput(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(20, 0))]);

    [Fact]
    public void Register_station_valid_command_passes()
    {
        new RegisterStationValidator().Validate(ValidStation()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Charging_station_without_connectors_fails()
    {
        var command = ValidStation() with { Connectors = [] };

        new RegisterStationValidator().Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Parking_only_station_needs_no_connectors()
    {
        var command = ValidStation() with { ListingType = ListingType.ParkingOnly, Connectors = [] };

        new RegisterStationValidator().Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Search_rejects_huge_radius_and_half_open_slot()
    {
        var validator = new SearchStationsValidator();

        validator.Validate(new SearchStationsQuery(33.5, 36.3, RadiusKm: 500)).IsValid.ShouldBeFalse();
        validator.Validate(new SearchStationsQuery(33.5, 36.3, From: DateTimeOffset.UtcNow)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Booking_rejects_wallet_payments_for_now()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new CreateBookingCommand(Guid.NewGuid(), Guid.NewGuid(), null, now.AddHours(1), now.AddHours(2), PaymentProvider.Wallet);

        new CreateBookingValidator().Validate(command).IsValid.ShouldBeFalse();
    }
}
