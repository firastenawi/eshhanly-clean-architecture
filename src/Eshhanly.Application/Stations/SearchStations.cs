using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Eshhanly.Application.Stations;

/// <summary>
/// Active stations within <see cref="RadiusKm"/> of a point, nearest first. Optional filters: plug type, minimum power,
/// and a time slot (then only stations open and with a free matching connector for the whole slot are returned).
/// </summary>
public sealed record SearchStationsQuery(
    double Latitude,
    double Longitude,
    double RadiusKm = 10,
    ConnectorType? ConnectorType = null,
    decimal? MinPowerKw = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<Result<IReadOnlyList<StationSearchResult>>>;

public sealed record StationSearchResult(
    Guid Id,
    string Title,
    string City,
    ListingType ListingType,
    double Latitude,
    double Longitude,
    double DistanceKm,
    decimal RatingAverage,
    int RatingCount,
    long FromPriceMinor,
    PricingModel PricingModel,
    string Currency,
    BookingMode BookingMode,
    IReadOnlyList<ConnectorSummary> Connectors);

public sealed record ConnectorSummary(Guid Id, ConnectorType Type, CurrentType CurrentType, decimal PowerKw);

internal sealed class SearchStationsValidator : AbstractValidator<SearchStationsQuery>
{
    public SearchStationsValidator()
    {
        RuleFor(q => q.Latitude).InclusiveBetween(-90, 90);
        RuleFor(q => q.Longitude).InclusiveBetween(-180, 180);
        RuleFor(q => q.RadiusKm).InclusiveBetween(0.1, 100);
        RuleFor(q => q.To).NotNull().GreaterThan(q => q.From).When(q => q.From.HasValue);
        RuleFor(q => q.From).NotNull().When(q => q.To.HasValue);
    }
}

internal sealed class SearchStationsHandler(IApplicationDbContext db)
    : IRequestHandler<SearchStationsQuery, Result<IReadOnlyList<StationSearchResult>>>
{
    private const int MaxResults = 50;

    private static readonly BookingStatus[] BlockingStatuses =
        [BookingStatus.Pending, BookingStatus.AwaitingHostApproval, BookingStatus.Confirmed, BookingStatus.InProgress];

    public async Task<Result<IReadOnlyList<StationSearchResult>>> Handle(SearchStationsQuery query, CancellationToken cancellationToken)
    {
        var origin = new Point(query.Longitude, query.Latitude) { SRID = Station.GeoSrid };
        var radiusMeters = query.RadiusKm * 1000;

        // 1) Geo filter in PostgreSQL (ST_DWithin / ST_Distance on geography → metres, GiST index).
        var nearby = db.Stations.AsNoTracking()
            .Where(s => s.Status == StationStatus.Active && s.Location.IsWithinDistance(origin, radiusMeters));

        if (query.ConnectorType is { } type)
        {
            nearby = nearby.Where(s => s.Connectors.Any(c => c.Type == type));
        }

        if (query.MinPowerKw is { } minPower)
        {
            nearby = nearby.Where(s => s.Connectors.Any(c => c.PowerKw >= minPower));
        }

        var hits = await nearby
            .Select(s => new { s.Id, Distance = s.Location.Distance(origin) })
            .OrderBy(s => s.Distance)
            .Take(MaxResults)
            .ToListAsync(cancellationToken);

        var ids = hits.Select(h => h.Id).ToList();

        // 2) Load the candidates with what the in-memory rules need.
        var stations = await db.Stations.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Include(s => s.Connectors)
            .Include(s => s.PricingRules)
            .Include(s => s.AvailabilityRules)
            .Include(s => s.AvailabilityOverrides)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        // 3) Optional slot filter: open for the whole slot and at least one matching connector not booked.
        TimeSlot? slot = null;
        HashSet<Guid> busyConnectors = [];
        if (query.From is { } from && query.To is { } to)
        {
            slot = TimeSlot.Create(from, to).Value;
            var connectorIds = stations.Values.SelectMany(s => s.Connectors).Select(c => c.Id).ToList();
            busyConnectors = (await db.Bookings.AsNoTracking()
                .Where(b => connectorIds.Contains(b.ConnectorId)
                    && BlockingStatuses.Contains(b.Status)
                    && b.StartAt < slot.End
                    && b.BlockedUntil > slot.Start)
                .Select(b => b.ConnectorId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        var results = new List<StationSearchResult>();
        foreach (var hit in hits)
        {
            var station = stations[hit.Id];
            var connectors = station.Connectors
                .Where(c => query.ConnectorType is null || c.Type == query.ConnectorType)
                .Where(c => query.MinPowerKw is null || c.PowerKw >= query.MinPowerKw)
                .ToList();

            if (slot is not null && (!station.IsOpenDuring(slot) || connectors.TrueForAll(c => busyConnectors.Contains(c.Id))))
            {
                continue;
            }

            var cheapest = station.PricingRules.MinBy(r => r.UnitPriceMinor)!;
            results.Add(new StationSearchResult(
                station.Id,
                station.Title,
                station.Address.City,
                station.ListingType,
                station.Location.Y,
                station.Location.X,
                Math.Round(hit.Distance / 1000, 2),
                station.RatingAverage,
                station.RatingCount,
                cheapest.UnitPriceMinor,
                cheapest.Model,
                station.Currency,
                station.BookingMode,
                connectors.Select(c => new ConnectorSummary(c.Id, c.Type, c.CurrentType, c.PowerKw)).ToList()));
        }

        return Result.Success<IReadOnlyList<StationSearchResult>>(results);
    }
}
