using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

/// <summary>A bookable plug (or the single virtual spot of a parking-only listing). Bookings target a connector.</summary>
public sealed class Connector : Entity
{
    private Connector() { }

    internal Connector(Guid stationId, int ocppConnectorId, ConnectorType type, CurrentType currentType, decimal powerKw)
    {
        StationId = stationId;
        OcppConnectorId = ocppConnectorId;
        Type = type;
        CurrentType = currentType;
        PowerKw = powerKw;
        Status = ConnectorStatus.Available;
    }

    public Guid StationId { get; private set; }

    /// <summary>OCPP connector number (1-based) on the charge box.</summary>
    public int OcppConnectorId { get; private set; }

    public ConnectorType Type { get; private set; }
    public CurrentType CurrentType { get; private set; }
    public decimal PowerKw { get; private set; }
    public ConnectorStatus Status { get; private set; }

    public bool IsVirtual => Type == ConnectorType.None;

    internal void SetStatus(ConnectorStatus status) => Status = status;
}
