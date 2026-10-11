using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Application.Common.Security;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Hosts;
using Eshhanly.Domain.Stations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Eshhanly.Application.Stations;

public sealed record ConnectorInput(ConnectorType Type, CurrentType CurrentType, decimal PowerKw);

public sealed record PricingInput(
    PricingModel Model,
    long UnitPriceMinor,
    WeekDays Days = WeekDays.All,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    int Priority = 0);

public sealed record AvailabilityInput(DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>
/// One-shot listing wizard: creates the station with its connectors, prices and weekly hours, and submits it for review.
/// The caller becomes a host on first use.
/// </summary>
public sealed record RegisterStationCommand(
    ListingType ListingType,
    string Title,
    string? Description,
    string? AccessInstructions,
    double Latitude,
    double Longitude,
    string AddressLine,
    string City,
    BookingMode BookingMode,
    IReadOnlyList<ConnectorInput> Connectors,
    IReadOnlyList<PricingInput> Pricing,
    IReadOnlyList<AvailabilityInput> Availability,
    string? HostDisplayName = null) : IRequest<Result<Guid>>;

internal sealed class RegisterStationValidator : AbstractValidator<RegisterStationCommand>
{
    public RegisterStationValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(Station.TitleMaxLength);
        RuleFor(c => c.AddressLine).NotEmpty().MaximumLength(Address.LineMaxLength);
        RuleFor(c => c.City).NotEmpty().MaximumLength(Address.CityMaxLength);
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);
        RuleFor(c => c.Pricing).NotEmpty();
        RuleFor(c => c.Availability).NotEmpty();
        RuleFor(c => c.Connectors)
            .NotEmpty().When(c => c.ListingType == ListingType.ChargingAndParking)
            .WithMessage("A charging listing needs at least one connector.");
    }
}

internal sealed class RegisterStationHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUserRoleService roles,
    TimeProvider clock) : IRequestHandler<RegisterStationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterStationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AppErrors.NotAuthenticated;
        }

        var host = await db.HostProfiles.FirstOrDefaultAsync(h => h.UserId == userId, cancellationToken);
        if (host is null)
        {
            var created = HostProfile.Create(userId, command.HostDisplayName ?? command.Title);
            if (created.IsFailure)
            {
                return created.Error;
            }

            host = created.Value;
            db.HostProfiles.Add(host);
            await roles.AddToRoleAsync(userId, Roles.Host, cancellationToken);
        }

        var details = new StationDetails(command.Title, command.Description, command.AccessInstructions, command.BookingMode);
        var stationResult = Station.Create(host.Id, command.ListingType, details, command.Latitude, command.Longitude,
            new Address(command.AddressLine, command.City));
        if (stationResult.IsFailure)
        {
            return stationResult.Error;
        }

        var station = stationResult.Value;

        foreach (var c in command.Connectors)
        {
            if (station.AddConnector(c.Type, c.CurrentType, c.PowerKw) is { IsFailure: true } r)
            {
                return r.Error;
            }
        }

        foreach (var p in command.Pricing)
        {
            if (station.AddPricingRule(p.Model, p.UnitPriceMinor, p.Days, p.StartTime, p.EndTime, p.Priority) is { IsFailure: true } r)
            {
                return r.Error;
            }
        }

        var availability = station.SetWeeklyAvailability(command.Availability.Select(a => (a.Day, a.StartTime, a.EndTime)));
        if (availability.IsFailure)
        {
            return availability.Error;
        }

        var submitted = station.SubmitForReview(clock.GetUtcNow());
        if (submitted.IsFailure)
        {
            return submitted.Error;
        }

        db.Stations.Add(station);
        await db.SaveChangesAsync(cancellationToken);
        return station.Id;
    }
}
