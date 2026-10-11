using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Bookings;

/// <summary>
/// Run every minute by a background worker:
/// expires bookings whose payment/approval deadline passed (and voids their hold), and marks no-shows.
/// </summary>
public sealed record ProcessBookingTimeoutsCommand : IRequest<int>;

internal sealed class ProcessBookingTimeoutsHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<ProcessBookingTimeoutsCommand, int>
{
    private const int BatchSize = 200;

    public async Task<int> Handle(ProcessBookingTimeoutsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var noShowCutoff = now - Booking.NoShowGracePeriod;

        var overdue = await db.Bookings
            .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.AwaitingHostApproval) && b.ExpiresAt <= now)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var booking in overdue)
        {
            booking.Expire(now);
            await CancelBookingHandler.VoidPaymentAsync(db, booking.Id, now, cancellationToken);
        }

        var noShows = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.StartAt <= noShowCutoff)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        // Payment stays authorized on purpose: the no-show fee policy (capture all/part) is a later decision.
        noShows.ForEach(b => b.MarkNoShow(now));

        var changed = overdue.Count + noShows.Count;
        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }
}
