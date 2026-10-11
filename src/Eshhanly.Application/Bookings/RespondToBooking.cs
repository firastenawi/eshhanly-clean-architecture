using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Bookings;

/// <summary>Host accepts or rejects a booking on a RequiresApproval station.</summary>
public sealed record RespondToBookingCommand(Guid BookingId, bool Approve, string? Reason = null) : IRequest<Result>;

internal sealed class RespondToBookingHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<RespondToBookingCommand, Result>
{
    public async Task<Result> Handle(RespondToBookingCommand command, CancellationToken cancellationToken)
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

        if (await BookingPreparation.ResolvePartyAsync(db, booking, userId, cancellationToken) != BookingParty.Host)
        {
            return AppErrors.NotHost;
        }

        var now = clock.GetUtcNow();
        var result = command.Approve ? booking.ApproveByHost(now) : booking.RejectByHost(command.Reason, now);
        if (result.IsFailure)
        {
            return result;
        }

        if (!command.Approve)
        {
            await CancelBookingHandler.VoidPaymentAsync(db, booking.Id, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
