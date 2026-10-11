using Eshhanly.Domain.Stations;

namespace Eshhanly.Domain.Bookings;

/// <summary>
/// Immutable snapshot of how a booking was priced at creation time (stored as jsonb),
/// so later price changes by the host never alter an existing booking.
/// </summary>
public sealed class PriceQuote
{
    public PricingModel Model { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal EstimatedKwh { get; init; }
    public long TotalMinor { get; init; }
    public List<PriceQuoteLine> Lines { get; init; } = [];
}

public sealed class PriceQuoteLine
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public long UnitPriceMinor { get; init; }
    public long AmountMinor { get; init; }
}
