using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Bookings;

internal sealed record PreparedBooking(Station Station, Connector Connector, Vehicle? Vehicle, TimeSlot Slot, PriceQuote Quote);

/// <summary>All checks shared by "get a quote" and "create a booking", so both always agree.</summary>
internal static class BookingPreparation
{
    public static readonly BookingStatus[] BlockingStatuses =
        [BookingStatus.Pending, BookingStatus.AwaitingHostApproval, BookingStatus.Confirmed, BookingStatus.InProgress];

    public static async Task<Result<PreparedBooking>> PrepareAsync(
        IApplicationDbContext db,
        Guid guestId,
        Guid stationId,
        Guid connectorId,
        Guid? vehicleId,
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var slotResult = TimeSlot.Create(start, end);
        if (slotResult.IsFailure)
        {
            return slotResult.Error;
        }

        var slot = slotResult.Value;
        if (slot.Start <= now)
        {
            return BookingErrors.StartInPast;
        }

        var station = await db.Stations
            .Include(s => s.Connectors)
            .Include(s => s.PricingRules)
            .Include(s => s.AvailabilityRules)
            .Include(s => s.AvailabilityOverrides)
            .FirstOrDefaultAsync(s => s.Id == stationId, cancellationToken);

        if (station is null)
        {
            return StationErrors.NotFound;
        }

        if (!station.IsBookable)
        {
            return BookingErrors.StationNotBookable;
        }

        var hostUserId = await db.HostProfiles.Where(h => h.Id == station.HostId).Select(h => h.UserId).FirstAsync(cancellationToken);
        if (hostUserId == guestId)
        {
            return BookingErrors.OwnStation;
        }

        var connector = station.Connectors.FirstOrDefault(c => c.Id == connectorId);
        if (connector is null)
        {
            return StationErrors.ConnectorNotFound;
        }

        Vehicle? vehicle = null;
        if (vehicleId is { } vid)
        {
            vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vid && v.OwnerId == guestId, cancellationToken);
            if (vehicle is null || !vehicle.CanUse(connector.Type))
            {
                return BookingErrors.IncompatibleVehicle;
            }
        }
        else if (!connector.IsVirtual)
        {
            return BookingErrors.VehicleRequired;
        }

        var minutes = slot.Duration.TotalMinutes;
        if (minutes < station.MinBookingMinutes || minutes > station.MaxBookingMinutes)
        {
            return BookingErrors.InvalidDuration;
        }

        if (!station.IsOpenDuring(slot))
        {
            return BookingErrors.StationClosed;
        }

        // Friendly pre-check; the DB exclusion constraint is the real guarantee under concurrency.
        var blockedUntil = slot.End.AddMinutes(station.BufferMinutes);
        var taken = await db.Bookings.AnyAsync(
            b => b.ConnectorId == connectorId
                && BlockingStatuses.Contains(b.Status)
                && b.StartAt < blockedUntil
                && b.BlockedUntil > slot.Start,
            cancellationToken);
        if (taken)
        {
            return BookingErrors.SlotTaken;
        }

        var quote = QuoteCalculator.Calculate(station, connector, vehicle, slot);
        if (quote.IsFailure)
        {
            return quote.Error;
        }

        return new PreparedBooking(station, connector, vehicle, slot, quote.Value);
    }

    /// <summary>Who is acting on a booking: the guest who made it, the host who owns the station, or neither.</summary>
    public static async Task<BookingParty?> ResolvePartyAsync(IApplicationDbContext db, Booking booking, Guid userId, CancellationToken cancellationToken)
    {
        if (booking.GuestId == userId)
        {
            return BookingParty.Guest;
        }

        var isHost = await db.Stations
            .Where(s => s.Id == booking.StationId)
            .Join(db.HostProfiles, s => s.HostId, h => h.Id, (s, h) => h.UserId)
            .AnyAsync(hostUserId => hostUserId == userId, cancellationToken);

        return isHost ? BookingParty.Host : null;
    }
}
