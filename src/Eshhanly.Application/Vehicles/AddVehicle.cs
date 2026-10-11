using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using Eshhanly.Domain.Vehicles;
using MediatR;

namespace Eshhanly.Application.Vehicles;

public sealed record AddVehicleCommand(
    string Make,
    string Model,
    string? PlateNumber,
    ConnectorType SupportedConnectors,
    decimal? BatteryKwh = null,
    decimal? MaxAcKw = null,
    decimal? MaxDcKw = null) : IRequest<Result<Guid>>;

internal sealed class AddVehicleHandler(IApplicationDbContext db, ICurrentUser currentUser)
    : IRequestHandler<AddVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddVehicleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AppErrors.NotAuthenticated;
        }

        var vehicle = Vehicle.Create(userId, command.Make, command.Model, command.PlateNumber, command.SupportedConnectors,
            command.BatteryKwh, command.MaxAcKw, command.MaxDcKw);
        if (vehicle.IsFailure)
        {
            return vehicle.Error;
        }

        db.Vehicles.Add(vehicle.Value);
        await db.SaveChangesAsync(cancellationToken);
        return vehicle.Value.Id;
    }
}
