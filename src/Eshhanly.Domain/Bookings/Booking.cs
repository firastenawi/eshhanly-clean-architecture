using System.Security.Cryptography;
using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Bookings;

/// <summary>
/// A guest's reservation of one connector for a time slot.
/// <code>
/// Pending ─(payment held)─► Confirmed ─(start)─► InProgress ─(stop)─► Completed
///    │           └─(approval needed)─► AwaitingHostApproval ─(approve)─► Confirmed
///    └─(deadline)─► Expired      Confirmed ─(grace elapsed)─► NoShow
/// Pending | AwaitingHostApproval | Confirmed ─(cancel/reject)─► Cancelled
/// </code>
/// </summary>
public sealed class Booking : AggregateRoot
{
    public const int NumberLength = 10;
    public const int CancellationReasonMaxLength = 500;

    /// <summary>Time the guest has to complete the payment hold.</summary>
    public static readonly TimeSpan PaymentHoldWindow = TimeSpan.FromMinutes(15);

    /// <summary>Time the host has to accept a RequiresApproval booking.</summary>
    public static readonly TimeSpan HostApprovalWindow = TimeSpan.FromHours(2);

    public static readonly TimeSpan EarlyStartAllowance = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan NoShowGracePeriod = TimeSpan.FromMinutes(30);

    private const string NumberAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I

    private Booking() { }

    /// <summary>Human-friendly reference shown to users, e.g. <c>ESH-7KQ2MX</c>.</summary>
    public string Number { get; private set; } = string.Empty;

    public Guid GuestId { get; private set; }
    public Guid StationId { get; private set; }
    public Guid ConnectorId { get; private set; }
    public Guid? VehicleId { get; private set; }

    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }

    /// <summary><see cref="EndAt"/> plus the station buffer. The DB exclusion constraint uses <c>[StartAt, BlockedUntil)</c>.</summary>
    public DateTimeOffset BlockedUntil { get; private set; }

    public BookingStatus Status { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public long EstimatedTotalMinor { get; private set; }
    public long? FinalTotalMinor { get; private set; }
    public PriceQuote Quote { get; private set; } = null!;

    /// <summary>Deadline of the current waiting state (payment hold or host approval).</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public BookingParty? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }

    public TimeSlot Slot => TimeSlot.Create(StartAt, EndAt).Value;

    /// <summary>
    /// Creates a booking after the caller (application layer) verified: station bookable &amp; open,
    /// slot free, vehicle compatible. The database exclusion constraint is the final guard against races.
    /// </summary>
    public static Result<Booking> Create(
        Guid guestId,
        Guid stationId,
        Guid connectorId,
        Guid? vehicleId,
        TimeSlot slot,
        int bufferMinutes,
        PriceQuote quote,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(quote);

        if (slot.Start <= now)
        {
            return BookingErrors.StartInPast;
        }

        var booking = new Booking
        {
            Number = GenerateNumber(),
            GuestId = guestId,
            StationId = stationId,
            ConnectorId = connectorId,
            VehicleId = vehicleId,
            StartAt = slot.Start,
            EndAt = slot.End,
            BlockedUntil = slot.End.AddMinutes(bufferMinutes),
            Status = BookingStatus.Pending,
            Currency = quote.Currency,
            EstimatedTotalMinor = quote.TotalMinor,
            Quote = quote,
            ExpiresAt = Min(now + PaymentHoldWindow, slot.Start),
        };

        booking.Raise(new BookingCreated(booking.Id, stationId, guestId, now));
        return booking;
    }

    /// <summary>Called once the payment provider confirmed the hold.</summary>
    public Result MarkPaymentHeld(bool requiresHostApproval, DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
        {
            return BookingErrors.InvalidTransition(Status, "hold payment for");
        }

        if (requiresHostApproval)
        {
            Status = BookingStatus.AwaitingHostApproval;
            ExpiresAt = Min(now + HostApprovalWindow, StartAt);
            Raise(new BookingAwaitingApproval(Id, StationId, now));
            return Result.Success();
        }

        Confirm(now);
        return Result.Success();
    }

    public Result ApproveByHost(DateTimeOffset now)
    {
        if (Status != BookingStatus.AwaitingHostApproval)
        {
            return BookingErrors.InvalidTransition(Status, "approve");
        }

        Confirm(now);
        return Result.Success();
    }

    public Result RejectByHost(string? reason, DateTimeOffset now)
    {
        if (Status != BookingStatus.AwaitingHostApproval)
        {
            return BookingErrors.InvalidTransition(Status, "reject");
        }

        return Cancel(BookingParty.Host, reason ?? "Rejected by host", now);
    }

    public Result Cancel(BookingParty by, string? reason, DateTimeOffset now)
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.AwaitingHostApproval or BookingStatus.Confirmed))
        {
            return BookingErrors.InvalidTransition(Status, "cancel");
        }

        var previous = Status;
        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = by;
        CancellationReason = reason?.Trim();
        ExpiresAt = null;
        Raise(new BookingCancelled(Id, by, previous, now));
        return Result.Success();
    }

    /// <summary>Background job: the payment hold or host approval deadline passed.</summary>
    public Result Expire(DateTimeOffset now)
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.AwaitingHostApproval))
        {
            return BookingErrors.InvalidTransition(Status, "expire");
        }

        if (ExpiresAt > now)
        {
            return BookingErrors.NotExpiredYet;
        }

        var previous = Status;
        Status = BookingStatus.Expired;
        ExpiresAt = null;
        Raise(new BookingExpired(Id, previous, now));
        return Result.Success();
    }

    public Result Start(DateTimeOffset now)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return BookingErrors.InvalidTransition(Status, "start");
        }

        if (now < StartAt - EarlyStartAllowance)
        {
            return BookingErrors.TooEarlyToStart;
        }

        Status = BookingStatus.InProgress;
        StartedAt = now;
        Raise(new BookingStarted(Id, now));
        return Result.Success();
    }

    public Result Complete(long finalTotalMinor, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(finalTotalMinor);

        if (Status != BookingStatus.InProgress)
        {
            return BookingErrors.InvalidTransition(Status, "complete");
        }

        Status = BookingStatus.Completed;
        FinalTotalMinor = finalTotalMinor;
        CompletedAt = now;
        Raise(new BookingCompleted(Id, finalTotalMinor, Currency, now));
        return Result.Success();
    }

    /// <summary>Background job: the guest never started the session within the grace period.</summary>
    public Result MarkNoShow(DateTimeOffset now)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return BookingErrors.InvalidTransition(Status, "mark as no-show");
        }

        if (now < StartAt + NoShowGracePeriod)
        {
            return BookingErrors.GracePeriodNotOver;
        }

        Status = BookingStatus.NoShow;
        Raise(new BookingMarkedNoShow(Id, now));
        return Result.Success();
    }

    private void Confirm(DateTimeOffset now)
    {
        Status = BookingStatus.Confirmed;
        ConfirmedAt = now;
        ExpiresAt = null;
        Raise(new BookingConfirmed(Id, StationId, GuestId, now));
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    private static string GenerateNumber() =>
        "ESH-" + RandomNumberGenerator.GetString(NumberAlphabet, NumberLength - 4);
}
