using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Bookings;

/// <summary>The signed-in guest's bookings, newest first.</summary>
public sealed record GetMyBookingsQuery : IRequest<Result<IReadOnlyList<BookingSummary>>>;

public sealed record BookingSummary(
    Guid Id,
    string Number,
    Guid StationId,
    string StationTitle,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    BookingStatus Status,
    long EstimatedTotalMinor,
    string Currency);

internal sealed class GetMyBookingsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyBookingsQuery, Result<IReadOnlyList<BookingSummary>>>
{
    public async Task<Result<IReadOnlyList<BookingSummary>>> Handle(GetMyBookingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AppErrors.NotAuthenticated;
        }

        var bookings = await (
                from b in db.Bookings.AsNoTracking()
                join s in db.Stations.IgnoreQueryFilters() on b.StationId equals s.Id
                where b.GuestId == userId
                orderby b.StartAt descending
                select new BookingSummary(b.Id, b.Number, s.Id, s.Title, b.StartAt, b.EndAt, b.Status, b.EstimatedTotalMinor, b.Currency))
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BookingSummary>>(bookings);
    }
}
