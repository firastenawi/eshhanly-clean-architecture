using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

public sealed class StationPhoto : Entity
{
    public const int UrlMaxLength = 500;

    private StationPhoto() { }

    internal StationPhoto(Guid stationId, string url, int sortOrder)
    {
        StationId = stationId;
        Url = url;
        SortOrder = sortOrder;
    }

    public Guid StationId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
}
