using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Bookings;

/// <summary>Guest or host cancels before the session starts. Nothing was captured yet, so the hold is simply voided.</summary>
public sealed record CancelBookingCommand(Guid BookingId, string? Reason = null) : IRequest<Result>;

internal sealed class CancelBookingHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CancelBookingCommand, Result>
{
    public async Task<Result> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AppErrors.NotAuthenticated;
        }

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == command.BookingId, cancellationToken);
        if (booking is null)
        {
            return BookingErrors.NotFound;
        }

        if (await BookingPreparation.ResolvePartyAsync(db, booking, userId, cancellationToken) is not { } party)
        {
            return AppErrors.NotAllowed;
        }

        var now = clock.GetUtcNow();
        var cancelled = booking.Cancel(party, command.Reason, now);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await VoidPaymentAsync(db, booking.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    internal static async Task VoidPaymentAsync(IApplicationDbContext db, Guid bookingId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);
        if (payment?.Status is PaymentStatus.Pending or PaymentStatus.Authorized)
        {
            payment.Void($"{bookingId}:void", now);
        }
    }
}
