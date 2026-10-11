using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

/// <summary>Recurring weekly opening window in station-local time. An <see cref="EndTime"/> of 00:00 means midnight (end of day).</summary>
public sealed class AvailabilityRule : Entity
{
    private AvailabilityRule() { }

    internal AvailabilityRule(Guid stationId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        StationId = stationId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public Guid StationId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    internal TimeSpan StartOffset => StartTime.ToTimeSpan();
    internal TimeSpan EndOffset => EndTime == TimeOnly.MinValue ? TimeSpan.FromDays(1) : EndTime.ToTimeSpan();
}

/// <summary>A one-off override of the weekly schedule: a blackout (holiday, maintenance) or an extra opening.</summary>
public sealed class AvailabilityOverride : Entity
{
    public const int ReasonMaxLength = 200;

    private AvailabilityOverride() { }

    internal AvailabilityOverride(Guid stationId, TimeSlot period, AvailabilityOverrideKind kind, string? reason)
    {
        StationId = stationId;
        StartAt = period.Start;
        EndAt = period.End;
        Kind = kind;
        Reason = reason;
    }

    public Guid StationId { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public AvailabilityOverrideKind Kind { get; private set; }
    public string? Reason { get; private set; }
}
