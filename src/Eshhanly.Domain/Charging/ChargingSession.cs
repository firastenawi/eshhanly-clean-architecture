using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Charging;

public enum SessionSource
{
    Simulated = 0,
    ManualConfirmation = 1,
    Ocpp = 2,
}

public enum SessionStatus
{
    Active = 0,
    Stopped = 1,
    Faulted = 2,
}

public sealed record ChargingSessionStarted(Guid SessionId, Guid BookingId, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record ChargingSessionStopped(Guid SessionId, Guid BookingId, long EnergyWh, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// The physical charging (or parking) session behind a booking. Energy comes from meter readings,
/// whether they are simulated, typed by the host, or reported by an OCPP charger.
/// </summary>
public sealed class ChargingSession : AggregateRoot
{
    private ChargingSession() { }

    public Guid BookingId { get; private set; }
    public Guid ConnectorId { get; private set; }
    public SessionSource Source { get; private set; }
    public SessionStatus Status { get; private set; }

    /// <summary>OCPP transactionId when <see cref="Source"/> is <see cref="SessionSource.Ocpp"/>.</summary>
    public int? OcppTransactionId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public long MeterStartWh { get; private set; }
    public long? MeterStopWh { get; private set; }
    public string? StopReason { get; private set; }

    public long EnergyWh => MeterStopWh is { } stop ? Math.Max(0, stop - MeterStartWh) : 0;

    public static ChargingSession Start(
        Guid bookingId,
        Guid connectorId,
        SessionSource source,
        long meterStartWh,
        DateTimeOffset now,
        int? ocppTransactionId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(meterStartWh);

        var session = new ChargingSession
        {
            BookingId = bookingId,
            ConnectorId = connectorId,
            Source = source,
            Status = SessionStatus.Active,
            StartedAt = now,
            MeterStartWh = meterStartWh,
            OcppTransactionId = ocppTransactionId,
        };

        session.Raise(new ChargingSessionStarted(session.Id, bookingId, now));
        return session;
    }

    public Result Stop(long meterStopWh, string? reason, DateTimeOffset now)
    {
        if (Status != SessionStatus.Active)
        {
            return Error.Conflict("ChargingSession.NotActive", "Only an active session can be stopped.");
        }

        if (meterStopWh < MeterStartWh)
        {
            return Error.Validation("ChargingSession.InvalidMeter", "Stop meter value cannot be lower than the start value.");
        }

        Status = SessionStatus.Stopped;
        MeterStopWh = meterStopWh;
        EndedAt = now;
        StopReason = reason;
        Raise(new ChargingSessionStopped(Id, BookingId, EnergyWh, now));
        return Result.Success();
    }

    public void MarkFaulted(string reason, DateTimeOffset now)
    {
        Status = SessionStatus.Faulted;
        EndedAt ??= now;
        StopReason = reason;
    }
}

/// <summary>
/// One telemetry sample. High-volume, append-only and queried by session: kept outside the aggregate
/// (never loaded through it) and keyed by a bigint identity instead of a Guid.
/// </summary>
public sealed class MeterValue
{
    private MeterValue() { }

    public MeterValue(Guid sessionId, DateTimeOffset timestamp, long energyWh, int? powerW, short? stateOfChargePercent)
    {
        SessionId = sessionId;
        Timestamp = timestamp;
        EnergyWh = energyWh;
        PowerW = powerW;
        StateOfChargePercent = stateOfChargePercent;
    }

    public long Id { get; private set; }
    public Guid SessionId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Cumulative energy register reading (Wh), as OCPP reports it.</summary>
    public long EnergyWh { get; private set; }

    public int? PowerW { get; private set; }
    public short? StateOfChargePercent { get; private set; }
}
