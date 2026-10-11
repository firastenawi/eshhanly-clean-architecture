using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Application.Common.Security;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Stations;

/// <summary>Admin approves (station goes live) or rejects (back to Draft) a submitted station.</summary>
public sealed record ReviewStationCommand(Guid StationId, bool Approve, string? Reason = null) : IRequest<Result>;

internal sealed class ReviewStationHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<ReviewStationCommand, Result>
{
    public async Task<Result> Handle(ReviewStationCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Admin))
        {
            return AppErrors.NotAllowed;
        }

        var station = await db.Stations.FirstOrDefaultAsync(s => s.Id == command.StationId, cancellationToken);
        if (station is null)
        {
            return StationErrors.NotFound;
        }

        var now = clock.GetUtcNow();
        var result = command.Approve ? station.Approve(now) : station.Reject(command.Reason ?? "Rejected", now);
        if (result.IsFailure)
        {
            return result;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
