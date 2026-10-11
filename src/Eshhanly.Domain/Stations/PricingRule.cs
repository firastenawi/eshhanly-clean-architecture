using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Stations;

/// <summary>
/// One price applying on <see cref="Days"/> between <see cref="StartTime"/> and <see cref="EndTime"/> (station-local).
/// Null times mean all day. When several rules match, the highest <see cref="Priority"/> wins.
/// </summary>
public sealed class PricingRule : Entity
{
    private PricingRule() { }

    internal PricingRule(
        Guid stationId,
        PricingModel model,
        long unitPriceMinor,
        WeekDays days,
        TimeOnly? startTime,
        TimeOnly? endTime,
        int priority,
        long? idleFeePerMinuteMinor)
    {
        StationId = stationId;
        Model = model;
        UnitPriceMinor = unitPriceMinor;
        Days = days;
        StartTime = startTime;
        EndTime = endTime;
        Priority = priority;
        IdleFeePerMinuteMinor = idleFeePerMinuteMinor;
    }

    public Guid StationId { get; private set; }
    public PricingModel Model { get; private set; }

    /// <summary>Price per hour, per kWh or per session depending on <see cref="Model"/>, in the station currency.</summary>
    public long UnitPriceMinor { get; private set; }

    public WeekDays Days { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public int Priority { get; private set; }

    /// <summary>Charged per minute once charging finished but the car still occupies the spot.</summary>
    public long? IdleFeePerMinuteMinor { get; private set; }

    public bool AppliesAt(DateTime localTime)
    {
        if (!Days.Includes(localTime.DayOfWeek))
        {
            return false;
        }

        if (StartTime is null || EndTime is null)
        {
            return true;
        }

        var time = TimeOnly.FromDateTime(localTime);
        return time >= StartTime && time < EndTime;
    }
}
