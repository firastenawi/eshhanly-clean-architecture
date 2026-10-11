using Eshhanly.Domain.Common;
using NetTopologySuite.Geometries;

namespace Eshhanly.Domain.Stations;

/// <summary>Host-editable listing settings, grouped to keep <see cref="Station.Create"/> readable.</summary>
public sealed record StationDetails(
    string Title,
    string? Description,
    string? AccessInstructions,
    BookingMode BookingMode = BookingMode.Instant,
    CancellationPolicy CancellationPolicy = CancellationPolicy.Moderate,
    int MinBookingMinutes = 30,
    int MaxBookingMinutes = 480,
    int BufferMinutes = 15);

/// <summary>
/// A host's listing at one location: its connectors, prices, weekly availability and photos.
/// Lifecycle: Draft → PendingReview → Active ⇄ Paused; admin can Suspend from any state.
/// </summary>
public sealed class Station : AggregateRoot
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 2000;
    public const int AccessInstructionsMaxLength = 1000;
    public const int MaxPhotos = 10;
    public const int GeoSrid = 4326;
    public const string DefaultTimeZoneId = "Asia/Damascus";

    private readonly List<Connector> _connectors = [];
    private readonly List<PricingRule> _pricingRules = [];
    private readonly List<AvailabilityRule> _availabilityRules = [];
    private readonly List<AvailabilityOverride> _availabilityOverrides = [];
    private readonly List<StationPhoto> _photos = [];

    private Station() { }

    public Guid HostId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? AccessInstructions { get; private set; }
    public ListingType ListingType { get; private set; }
    public StationStatus Status { get; private set; }

    /// <summary>WGS84 point (X = longitude, Y = latitude).</summary>
    public Point Location { get; private set; } = null!;

    public Address Address { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = DefaultTimeZoneId;
    public string Currency { get; private set; } = Currencies.SyrianPound;
    public BookingMode BookingMode { get; private set; }
    public CancellationPolicy CancellationPolicy { get; private set; }
    public int MinBookingMinutes { get; private set; }
    public int MaxBookingMinutes { get; private set; }
    public int BufferMinutes { get; private set; }

    /// <summary>OCPP charge box identity for smart chargers; null when sessions are confirmed manually/simulated.</summary>
    public string? OcppChargeBoxId { get; private set; }

    public decimal RatingAverage { get; private set; }
    public int RatingCount { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Connector> Connectors => _connectors.AsReadOnly();
    public IReadOnlyCollection<PricingRule> PricingRules => _pricingRules.AsReadOnly();
    public IReadOnlyCollection<AvailabilityRule> AvailabilityRules => _availabilityRules.AsReadOnly();
    public IReadOnlyCollection<AvailabilityOverride> AvailabilityOverrides => _availabilityOverrides.AsReadOnly();
    public IReadOnlyCollection<StationPhoto> Photos => _photos.AsReadOnly();

    public bool IsBookable => Status == StationStatus.Active && !IsDeleted;

    // ---------- Creation & details ----------

    public static Result<Station> Create(
        Guid hostId,
        ListingType listingType,
        StationDetails details,
        double latitude,
        double longitude,
        Address address,
        string timeZoneId = DefaultTimeZoneId,
        string currency = Currencies.SyrianPound)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(address);

        if (ValidateDetails(details) is { } detailsError)
        {
            return detailsError;
        }

        if (ValidateLocation(latitude, longitude, address) is { } locationError)
        {
            return locationError;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
        {
            return StationErrors.InvalidTimeZone;
        }

        var station = new Station
        {
            HostId = hostId,
            ListingType = listingType,
            Status = StationStatus.Draft,
            TimeZoneId = timeZoneId,
            Currency = Currencies.Normalize(currency),
        };
        station.ApplyDetails(details);
        station.ApplyLocation(latitude, longitude, address);

        if (listingType == ListingType.ParkingOnly)
        {
            station._connectors.Add(new Connector(station.Id, 1, ConnectorType.None, CurrentType.Ac, 0));
        }

        return station;
    }

    public Result UpdateDetails(StationDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        if (ValidateDetails(details) is { } error)
        {
            return error;
        }

        ApplyDetails(details);
        return Result.Success();
    }

    public Result UpdateLocation(double latitude, double longitude, Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        if (ValidateLocation(latitude, longitude, address) is { } error)
        {
            return error;
        }

        ApplyLocation(latitude, longitude, address);
        return Result.Success();
    }

    public void LinkOcppChargeBox(string? chargeBoxId) => OcppChargeBoxId = string.IsNullOrWhiteSpace(chargeBoxId) ? null : chargeBoxId.Trim();

    // ---------- Connectors ----------

    public Result<Connector> AddConnector(ConnectorType type, CurrentType currentType, decimal powerKw)
    {
        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        if (ListingType == ListingType.ParkingOnly)
        {
            return StationErrors.ParkingOnlyConnectors;
        }

        if (type == ConnectorType.None || !Enum.IsDefined(type))
        {
            return StationErrors.ConnectorTypeRequired;
        }

        if (powerKw is < 1 or > 400)
        {
            return StationErrors.InvalidPower;
        }

        var nextOcppId = _connectors.Count == 0 ? 1 : _connectors.Max(c => c.OcppConnectorId) + 1;
        var connector = new Connector(Id, nextOcppId, type, currentType, powerKw);
        _connectors.Add(connector);
        return connector;
    }

    public Result RemoveConnector(Guid connectorId)
    {
        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        var connector = _connectors.Find(c => c.Id == connectorId);
        if (connector is null)
        {
            return StationErrors.ConnectorNotFound;
        }

        if (_connectors.Count == 1 && Status != StationStatus.Draft)
        {
            return StationErrors.LastConnector;
        }

        _connectors.Remove(connector);
        return Result.Success();
    }

    public Result SetConnectorStatus(Guid connectorId, ConnectorStatus status)
    {
        var connector = _connectors.Find(c => c.Id == connectorId);
        if (connector is null)
        {
            return StationErrors.ConnectorNotFound;
        }

        connector.SetStatus(status);
        return Result.Success();
    }

    // ---------- Pricing ----------

    public Result<PricingRule> AddPricingRule(
        PricingModel model,
        long unitPriceMinor,
        WeekDays days = WeekDays.All,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        int priority = 0,
        long? idleFeePerMinuteMinor = null)
    {
        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        if (unitPriceMinor <= 0 || idleFeePerMinuteMinor < 0)
        {
            return StationErrors.InvalidPrice;
        }

        var hasOneTimeOnly = startTime.HasValue != endTime.HasValue;
        if (days == WeekDays.None || hasOneTimeOnly || (startTime.HasValue && startTime >= endTime))
        {
            return StationErrors.InvalidPricingWindow;
        }

        if (model == PricingModel.PerKwh && ListingType == ListingType.ParkingOnly)
        {
            return StationErrors.PerKwhOnParking;
        }

        var rule = new PricingRule(Id, model, unitPriceMinor, days, startTime, endTime, priority, idleFeePerMinuteMinor);
        _pricingRules.Add(rule);
        return rule;
    }

    public Result RemovePricingRule(Guid ruleId)
    {
        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        return _pricingRules.RemoveAll(r => r.Id == ruleId) == 0
            ? StationErrors.PricingRuleNotFound
            : Result.Success();
    }

    /// <summary>The rule that prices the given station-local moment, or null when none matches.</summary>
    public PricingRule? ResolvePricingRule(DateTime localTime) =>
        _pricingRules
            .Where(r => r.AppliesAt(localTime))
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

    // ---------- Availability ----------

    /// <summary>Replaces the whole weekly schedule.</summary>
    public Result SetWeeklyAvailability(IEnumerable<(DayOfWeek Day, TimeOnly Start, TimeOnly End)> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        var rules = windows.Select(w => new AvailabilityRule(Id, w.Day, w.Start, w.End)).ToList();

        if (rules.Exists(r => r.StartOffset >= r.EndOffset))
        {
            return StationErrors.InvalidAvailabilityWindow;
        }

        var overlaps = rules
            .GroupBy(r => r.DayOfWeek)
            .Any(day => day
                .OrderBy(r => r.StartOffset)
                .Zip(day.OrderBy(r => r.StartOffset).Skip(1))
                .Any(pair => pair.Second.StartOffset < pair.First.EndOffset));

        if (overlaps)
        {
            return StationErrors.OverlappingAvailability;
        }

        _availabilityRules.Clear();
        _availabilityRules.AddRange(rules);
        return Result.Success();
    }

    public Result<AvailabilityOverride> AddAvailabilityOverride(TimeSlot period, AvailabilityOverrideKind kind, string? reason)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (EnsureEditable() is { } stateError)
        {
            return stateError;
        }

        var availabilityOverride = new AvailabilityOverride(Id, period, kind, reason?.Trim());
        _availabilityOverrides.Add(availabilityOverride);
        return availabilityOverride;
    }

    public Result RemoveAvailabilityOverride(Guid overrideId) =>
        _availabilityOverrides.RemoveAll(e => e.Id == overrideId) == 0
            ? StationErrors.OverrideNotFound
            : Result.Success();

    /// <summary>
    /// True when the whole slot falls inside opening hours (weekly rules + extra openings) and touches no blackout.
    /// Existing bookings are checked separately (and enforced by the database exclusion constraint).
    /// </summary>
    public bool IsOpenDuring(TimeSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var blocked = _availabilityOverrides.Any(e =>
            e.Kind == AvailabilityOverrideKind.Blocked && e.StartAt < slot.End && slot.Start < e.EndAt);
        if (blocked)
        {
            return false;
        }

        var coveredByExtra = _availabilityOverrides.Any(e =>
            e.Kind == AvailabilityOverrideKind.ExtraOpen && e.StartAt <= slot.Start && slot.End <= e.EndAt);
        if (coveredByExtra)
        {
            return true;
        }

        // Walk the slot day by day in station-local time; every piece must sit inside one weekly window.
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(slot.Start, zone).DateTime;
        var localEnd = TimeZoneInfo.ConvertTime(slot.End, zone).DateTime;

        for (var cursor = localStart; cursor < localEnd;)
        {
            var dayStart = cursor.Date;
            var pieceEnd = localEnd < dayStart.AddDays(1) ? localEnd : dayStart.AddDays(1);
            var from = cursor - dayStart;
            var to = pieceEnd - dayStart;

            var fits = _availabilityRules.Exists(r =>
                r.DayOfWeek == dayStart.DayOfWeek && r.StartOffset <= from && to <= r.EndOffset);
            if (!fits)
            {
                return false;
            }

            cursor = pieceEnd;
        }

        return true;
    }

    // ---------- Photos ----------

    public Result<StationPhoto> AddPhoto(string url)
    {
        if (Guard.Required(url, "PhotoUrl", StationPhoto.UrlMaxLength) is { } error)
        {
            return error;
        }

        if (_photos.Count >= MaxPhotos)
        {
            return StationErrors.TooManyPhotos;
        }

        var photo = new StationPhoto(Id, url.Trim(), _photos.Count);
        _photos.Add(photo);
        return photo;
    }

    public Result RemovePhoto(Guid photoId) =>
        _photos.RemoveAll(p => p.Id == photoId) == 0 ? StationErrors.PhotoNotFound : Result.Success();

    // ---------- Lifecycle ----------

    public Result SubmitForReview(DateTimeOffset now)
    {
        if (Status != StationStatus.Draft)
        {
            return StationErrors.InvalidTransition(Status, "submit");
        }

        if (_connectors.Count == 0 || _pricingRules.Count == 0 || _availabilityRules.Count == 0)
        {
            return StationErrors.NotReadyForReview;
        }

        Status = StationStatus.PendingReview;
        Raise(new StationSubmittedForReview(Id, HostId, now));
        return Result.Success();
    }

    public Result Approve(DateTimeOffset now)
    {
        if (Status != StationStatus.PendingReview)
        {
            return StationErrors.InvalidTransition(Status, "approve");
        }

        Status = StationStatus.Active;
        Raise(new StationApproved(Id, HostId, now));
        return Result.Success();
    }

    public Result Reject(string reason, DateTimeOffset now)
    {
        if (Status != StationStatus.PendingReview)
        {
            return StationErrors.InvalidTransition(Status, "reject");
        }

        Status = StationStatus.Draft;
        Raise(new StationRejected(Id, HostId, reason, now));
        return Result.Success();
    }

    public Result Pause()
    {
        if (Status != StationStatus.Active)
        {
            return StationErrors.InvalidTransition(Status, "pause");
        }

        Status = StationStatus.Paused;
        return Result.Success();
    }

    public Result Resume()
    {
        if (Status != StationStatus.Paused)
        {
            return StationErrors.InvalidTransition(Status, "resume");
        }

        Status = StationStatus.Active;
        return Result.Success();
    }

    public Result Suspend(string reason, DateTimeOffset now)
    {
        if (Status == StationStatus.Suspended)
        {
            return StationErrors.InvalidTransition(Status, "suspend");
        }

        Status = StationStatus.Suspended;
        Raise(new StationSuspended(Id, HostId, reason, now));
        return Result.Success();
    }

    /// <summary>Admin lifts a suspension; the station goes back to Paused so the host re-activates it consciously.</summary>
    public Result Reinstate()
    {
        if (Status != StationStatus.Suspended)
        {
            return StationErrors.InvalidTransition(Status, "reinstate");
        }

        Status = StationStatus.Paused;
        return Result.Success();
    }

    public void Delete() => IsDeleted = true;

    public void ApplyRating(int rating)
    {
        RatingAverage = Math.Round(((RatingAverage * RatingCount) + rating) / (RatingCount + 1), 2);
        RatingCount++;
    }

    // ---------- Helpers ----------

    private Error? EnsureEditable() => Status == StationStatus.Suspended ? StationErrors.Suspended : null;

    private static Error? ValidateDetails(StationDetails details)
    {
        var error = Guard.Required(details.Title, nameof(Title), TitleMaxLength);
        if (error is not null)
        {
            return error;
        }

        if (details.Description?.Length > DescriptionMaxLength || details.AccessInstructions?.Length > AccessInstructionsMaxLength)
        {
            return Error.Validation("Station.TextTooLong", "Description or access instructions are too long.");
        }

        var limitsValid = details.MinBookingMinutes >= 15
            && details.MinBookingMinutes <= details.MaxBookingMinutes
            && details.MaxBookingMinutes <= 1440
            && details.BufferMinutes is >= 0 and <= 120;

        return limitsValid ? null : StationErrors.InvalidBookingLimits;
    }

    private static Error? ValidateLocation(double latitude, double longitude, Address address)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || double.IsNaN(latitude) || double.IsNaN(longitude))
        {
            return StationErrors.InvalidCoordinates;
        }

        return Guard.Required(address.Line, "AddressLine", Address.LineMaxLength)
            ?? Guard.Required(address.City, "City", Address.CityMaxLength)
            ?? (address.Country?.Length == 2 ? null : Error.Validation("Country.Invalid", "Country must be an ISO-3166 alpha-2 code."));
    }

    private void ApplyDetails(StationDetails details)
    {
        Title = details.Title.Trim();
        Description = details.Description?.Trim();
        AccessInstructions = details.AccessInstructions?.Trim();
        BookingMode = details.BookingMode;
        CancellationPolicy = details.CancellationPolicy;
        MinBookingMinutes = details.MinBookingMinutes;
        MaxBookingMinutes = details.MaxBookingMinutes;
        BufferMinutes = details.BufferMinutes;
    }

    private void ApplyLocation(double latitude, double longitude, Address address)
    {
        Location = new Point(longitude, latitude) { SRID = GeoSrid };
        Address = address with { Line = address.Line.Trim(), City = address.City.Trim(), Country = address.Country.ToUpperInvariant() };
    }
}
