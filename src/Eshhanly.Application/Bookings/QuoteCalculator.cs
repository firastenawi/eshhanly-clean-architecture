using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;

namespace Eshhanly.Application.Bookings;

/// <summary>
/// Simple pricing: the rule active at the slot's local start time prices the whole slot.
/// PerHour = hours × price; PerKwh = estimated kWh (power × hours) × price; PerSession = flat price.
/// </summary>
internal static class QuoteCalculator
{
    public static readonly Error NoPricing = Error.Validation("Booking.NoPricing", "The station has no price for the requested time.");

    public static Result<PriceQuote> Calculate(Station station, Connector connector, Vehicle? vehicle, TimeSlot slot)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(station.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(slot.Start, zone).DateTime;

        var rule = station.ResolvePricingRule(localStart);
        if (rule is null)
        {
            return NoPricing;
        }

        var hours = Math.Round((decimal)slot.Duration.TotalHours, 2);

        var kwh = rule.Model == PricingModel.PerKwh ? EstimateEnergy(connector, vehicle, hours) : 0m;
        var (quantity, unit, description) = rule.Model switch
        {
            PricingModel.PerKwh => (kwh, "kWh", "Estimated energy"),
            PricingModel.PerSession => (1m, "session", "Session fee"),
            _ => (hours, "h", "Parking & charging time"),
        };

        var total = (long)Math.Ceiling(quantity * rule.UnitPriceMinor);

        return new PriceQuote
        {
            Model = rule.Model,
            Currency = station.Currency,
            EstimatedKwh = kwh,
            TotalMinor = total,
            Lines =
            [
                new PriceQuoteLine
                {
                    Description = description,
                    Quantity = quantity,
                    Unit = unit,
                    UnitPriceMinor = rule.UnitPriceMinor,
                    AmountMinor = total,
                },
            ],
        };
    }

    /// <summary>Charger power capped by what the vehicle accepts for that current type.</summary>
    private static decimal EstimateEnergy(Connector connector, Vehicle? vehicle, decimal hours)
    {
        var vehicleMax = connector.CurrentType == CurrentType.Dc ? vehicle?.MaxDcKw : vehicle?.MaxAcKw;
        var power = vehicleMax is { } max ? Math.Min(connector.PowerKw, max) : connector.PowerKw;
        return Math.Round(power * hours, 1);
    }
}
