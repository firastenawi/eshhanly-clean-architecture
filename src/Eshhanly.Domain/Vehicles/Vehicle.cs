using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;

namespace Eshhanly.Domain.Vehicles;

public sealed class Vehicle : AggregateRoot
{
    private Vehicle() { }

    public Guid OwnerId { get; private set; }
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? PlateNumber { get; private set; }
    public ConnectorType SupportedConnectors { get; private set; }
    public decimal? BatteryKwh { get; private set; }
    public decimal? MaxAcKw { get; private set; }
    public decimal? MaxDcKw { get; private set; }
    public bool IsDeleted { get; private set; }

    public static Result<Vehicle> Create(
        Guid ownerId,
        string make,
        string model,
        string? plateNumber,
        ConnectorType supportedConnectors,
        decimal? batteryKwh = null,
        decimal? maxAcKw = null,
        decimal? maxDcKw = null)
    {
        if (Guard.Required(make, nameof(Make), 50) is { } makeError)
        {
            return makeError;
        }

        if (Guard.Required(model, nameof(Model), 50) is { } modelError)
        {
            return modelError;
        }

        if (supportedConnectors == ConnectorType.None)
        {
            return Error.Validation("Vehicle.NoConnectors", "At least one supported connector type is required.");
        }

        return new Vehicle
        {
            OwnerId = ownerId,
            Make = make.Trim(),
            Model = model.Trim(),
            PlateNumber = plateNumber?.Trim(),
            SupportedConnectors = supportedConnectors,
            BatteryKwh = batteryKwh,
            MaxAcKw = maxAcKw,
            MaxDcKw = maxDcKw,
        };
    }

    public bool CanUse(ConnectorType connector) =>
        connector == ConnectorType.None || SupportedConnectors.HasFlag(connector);

    public void Delete() => IsDeleted = true;
}
