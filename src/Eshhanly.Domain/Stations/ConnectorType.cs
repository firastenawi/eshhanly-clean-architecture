namespace Eshhanly.Domain.Stations;

/// <summary>
/// Plug standards. Flags so a <see cref="Vehicles.Vehicle"/> can declare every plug it accepts,
/// while a <see cref="Connector"/> always holds exactly one value.
/// </summary>
[Flags]
public enum ConnectorType
{
    /// <summary>Virtual connector of a parking-only listing.</summary>
    None = 0,
    Type1 = 1 << 0,
    Type2 = 1 << 1,
    Ccs1 = 1 << 2,
    Ccs2 = 1 << 3,
    Chademo = 1 << 4,
    GbtAc = 1 << 5,
    GbtDc = 1 << 6,
    Schuko = 1 << 7,
}

public enum CurrentType
{
    Ac = 0,
    Dc = 1,
}
