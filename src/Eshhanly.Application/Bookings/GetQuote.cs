using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using MediatR;

namespace Eshhanly.Application.Bookings;

/// <summary>Price preview shown before the guest confirms. Runs exactly the same checks as <see cref="CreateBookingCommand"/>.</summary>
public sealed record GetQuoteQuery(Guid StationId, Guid ConnectorId, Guid? VehicleId, DateTimeOffset Start, DateTimeOffset End)
    : IRequest<Result<PriceQuote>>;

internal sealed class GetQuoteHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<GetQuoteQuery, Result<PriceQuote>>
{
    public async Task<Result<PriceQuote>> Handle(GetQuoteQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AppErrors.NotAuthenticated;
        }

        var prepared = await BookingPreparation.PrepareAsync(db, userId, query.StationId, query.ConnectorId, query.VehicleId,
            query.Start, query.End, clock.GetUtcNow(), cancellationToken);

        return prepared.IsSuccess ? prepared.Value.Quote : prepared.Error;
    }
}
