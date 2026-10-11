using Eshhanly.Application.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;

namespace Eshhanly.Application.Tests.Bookings;

public class QuoteCalculatorTests
{
    // Sunday 2026-10-11 10:00 Damascus (UTC+3).
    private static readonly DateTimeOffset Start = new(2026, 10, 11, 7, 0, 0, TimeSpan.Zero);

    private static (Station Station, Connector Connector) StationWith(PricingModel model, long price, decimal powerKw = 11, CurrentType current = CurrentType.Ac)
    {
        var station = Station.Create(Guid.NewGuid(), ListingType.ChargingAndParking, new StationDetails("Test", null, null),
            33.5, 36.3, new Address("Street", "Damascus")).Value;
        var connector = station.AddConnector(current == CurrentType.Dc ? ConnectorType.Ccs2 : ConnectorType.Type2, current, powerKw).Value;
        station.AddPricingRule(model, price);
        return (station, connector);
    }

    private static TimeSlot Hours(double hours) => TimeSlot.Create(Start, Start.AddHours(hours)).Value;

    [Fact]
    public void Per_hour_multiplies_duration()
    {
        var (station, connector) = StationWith(PricingModel.PerHour, 5_000);

        var quote = QuoteCalculator.Calculate(station, connector, null, Hours(2.5)).Value;

        quote.TotalMinor.ShouldBe(12_500);
        quote.Currency.ShouldBe("SYP");
        quote.Lines.ShouldHaveSingleItem().Unit.ShouldBe("h");
    }

    [Fact]
    public void Per_kwh_estimates_energy_from_charger_power()
    {
        var (station, connector) = StationWith(PricingModel.PerKwh, 1_000, powerKw: 11);

        var quote = QuoteCalculator.Calculate(station, connector, null, Hours(2)).Value;

        quote.EstimatedKwh.ShouldBe(22m);
        quote.TotalMinor.ShouldBe(22_000);
    }

    [Fact]
    public void Per_kwh_is_capped_by_vehicle_onboard_charger()
    {
        var (station, connector) = StationWith(PricingModel.PerKwh, 1_000, powerKw: 22);
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Tesla", "Model 3", null, ConnectorType.Type2, maxAcKw: 11).Value;

        var quote = QuoteCalculator.Calculate(station, connector, vehicle, Hours(1)).Value;

        quote.EstimatedKwh.ShouldBe(11m);
    }

    [Fact]
    public void Per_session_is_flat()
    {
        var (station, connector) = StationWith(PricingModel.PerSession, 20_000);

        QuoteCalculator.Calculate(station, connector, null, Hours(3)).Value.TotalMinor.ShouldBe(20_000);
    }

    [Fact]
    public void Peak_rule_applies_by_local_start_time()
    {
        var (station, connector) = StationWith(PricingModel.PerHour, 5_000);
        station.AddPricingRule(PricingModel.PerHour, 9_000, WeekDays.All, new TimeOnly(9, 0), new TimeOnly(12, 0), priority: 5);

        QuoteCalculator.Calculate(station, connector, null, Hours(1)).Value.TotalMinor.ShouldBe(9_000);
    }

    [Fact]
    public void No_matching_rule_is_an_error()
    {
        var (station, connector) = StationWith(PricingModel.PerHour, 5_000);
        station.RemovePricingRule(station.PricingRules.Single().Id);

        QuoteCalculator.Calculate(station, connector, null, Hours(1)).Error.ShouldBe(QuoteCalculator.NoPricing);
    }
}
