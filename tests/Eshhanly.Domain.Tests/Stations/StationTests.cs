using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;

namespace Eshhanly.Domain.Tests.Stations;

public class StationTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Create_validates_coordinates()
    {
        var result = Station.Create(Guid.NewGuid(), ListingType.ChargingAndParking, new StationDetails("X", null, null),
            latitude: 120, longitude: 36, new Address("Street", "Damascus"));

        result.Error.ShouldBe(StationErrors.InvalidCoordinates);
    }

    [Fact]
    public void Create_stores_point_as_lon_lat_wgs84()
    {
        var station = TestData.NewStation();

        station.Location.X.ShouldBe(36.2765);
        station.Location.Y.ShouldBe(33.5138);
        station.Location.SRID.ShouldBe(4326);
        station.Status.ShouldBe(StationStatus.Draft);
        station.Currency.ShouldBe("SYP");
    }

    [Fact]
    public void Parking_only_gets_a_single_virtual_connector_and_rejects_real_ones()
    {
        var station = TestData.NewStation(ListingType.ParkingOnly);

        station.Connectors.ShouldHaveSingleItem().IsVirtual.ShouldBeTrue();
        station.AddConnector(ConnectorType.Type2, CurrentType.Ac, 7).Error.ShouldBe(StationErrors.ParkingOnlyConnectors);
        station.AddPricingRule(PricingModel.PerKwh, 100).Error.ShouldBe(StationErrors.PerKwhOnParking);
    }

    [Fact]
    public void Connectors_get_sequential_ocpp_ids()
    {
        var station = TestData.NewStation();

        station.AddConnector(ConnectorType.Type2, CurrentType.Ac, 7.4m).Value.OcppConnectorId.ShouldBe(1);
        station.AddConnector(ConnectorType.Ccs2, CurrentType.Dc, 50).Value.OcppConnectorId.ShouldBe(2);
    }

    [Fact]
    public void Submit_requires_connector_pricing_and_availability()
    {
        TestData.NewStation().SubmitForReview(Now).Error.ShouldBe(StationErrors.NotReadyForReview);

        var ready = TestData.NewReadyStation();
        ready.SubmitForReview(Now).IsSuccess.ShouldBeTrue();
        ready.Approve(Now).IsSuccess.ShouldBeTrue();
        ready.IsBookable.ShouldBeTrue();
    }

    [Fact]
    public void Suspended_station_cannot_be_edited()
    {
        var station = TestData.NewReadyStation();
        station.Suspend("Fraud report", Now);

        station.AddConnector(ConnectorType.Type2, CurrentType.Ac, 11).Error.ShouldBe(StationErrors.Suspended);
        station.IsBookable.ShouldBeFalse();
    }

    [Fact]
    public void Weekly_availability_rejects_overlapping_windows()
    {
        var station = TestData.NewStation();

        var result = station.SetWeeklyAvailability(
        [
            (DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            (DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(15, 0)),
        ]);

        result.Error.ShouldBe(StationErrors.OverlappingAvailability);
    }

    [Theory]
    [InlineData(2, 2, true)]   // 10:00-12:00 local, inside 08:00-22:00
    [InlineData(13, 2, false)] // 21:00-23:00 local, runs past closing
    [InlineData(-1, 1, false)] // 07:00-08:00 local, before opening
    public void IsOpenDuring_respects_local_weekly_hours(int startHoursFromNow, int hours, bool expected)
    {
        var station = TestData.NewReadyStation();

        station.IsOpenDuring(TestData.Slot(startHoursFromNow, hours)).ShouldBe(expected);
    }

    [Fact]
    public void Midnight_end_means_end_of_day_and_slots_can_cross_into_next_day()
    {
        var station = TestData.NewStation();
        station.SetWeeklyAvailability(Enum.GetValues<DayOfWeek>().Select(d => (d, TimeOnly.MinValue, TimeOnly.MinValue)));

        station.IsOpenDuring(TestData.Slot(14, 4)).ShouldBeTrue(); // 22:00 → 02:00 local
    }

    [Fact]
    public void Blocked_override_closes_station()
    {
        var station = TestData.NewReadyStation();
        station.AddAvailabilityOverride(TestData.Slot(3, 1), AvailabilityOverrideKind.Blocked, "Maintenance");

        station.IsOpenDuring(TestData.Slot(2, 3)).ShouldBeFalse();
        station.IsOpenDuring(TestData.Slot(5, 1)).ShouldBeTrue();
    }

    [Fact]
    public void Highest_priority_pricing_rule_wins()
    {
        var station = TestData.NewReadyStation(); // base 5000/h, priority 0
        station.AddPricingRule(PricingModel.PerHour, 8_000, WeekDays.All, new TimeOnly(17, 0), new TimeOnly(21, 0), priority: 10);

        station.ResolvePricingRule(new DateTime(2026, 10, 11, 18, 0, 0))!.UnitPriceMinor.ShouldBe(8_000);
        station.ResolvePricingRule(new DateTime(2026, 10, 11, 10, 0, 0))!.UnitPriceMinor.ShouldBe(5_000);
    }

    [Fact]
    public void TimeSlot_rejects_inverted_range()
    {
        TimeSlot.Create(Now, Now.AddHours(-1)).IsFailure.ShouldBeTrue();
    }
}
