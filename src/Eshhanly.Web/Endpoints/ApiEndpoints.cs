using Eshhanly.Application.Bookings;
using Eshhanly.Application.Common.Security;
using Eshhanly.Application.Stations;
using Eshhanly.Application.Vehicles;
using Eshhanly.Domain.Stations;
using Eshhanly.Infrastructure.Identity;
using MediatR;

namespace Eshhanly.Web.Endpoints;

internal static class ApiEndpoints
{
    public sealed record ReviewRequest(bool Approve, string? Reason);
    public sealed record CancelRequest(string? Reason);
    public sealed record RespondRequest(bool Approve, string? Reason);

    /// <summary>REST API for the web UI and the future mobile app, under <c>/api/v1</c>.</summary>
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1").AddEndpointFilter<ValidationExceptionFilter>();

        v1.MapGet("/meta", () => TypedResults.Ok(new { Name = "Eshhanly", Version = "v1" })).WithTags("Meta");

        // POST /register, /login, /refresh, GET /manage/info, POST /manage/2fa, ...
        v1.MapGroup("/auth").WithTags("Auth").MapIdentityApi<ApplicationUser>();

        MapStations(v1.MapGroup("/stations").WithTags("Stations"));
        MapBookings(v1.MapGroup("/bookings").WithTags("Bookings").RequireAuthorization());
        MapVehicles(v1.MapGroup("/vehicles").WithTags("Vehicles").RequireAuthorization());

        return app;
    }

    private static void MapStations(RouteGroupBuilder stations)
    {
        stations.MapGet("/search", async (
                double lat, double lng, double? radiusKm, ConnectorType? connectorType, decimal? minPowerKw,
                DateTimeOffset? from, DateTimeOffset? to, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SearchStationsQuery(lat, lng, radiusKm ?? 10, connectorType, minPowerKw, from, to), ct)).ToHttp())
            .WithSummary("Find active stations near a point, optionally free for a time slot")
            .AllowAnonymous();

        stations.MapPost("/", async (RegisterStationCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(id => TypedResults.Created($"/api/v1/stations/{id}", new { id })))
            .WithSummary("Register a station (caller becomes a host) and submit it for review")
            .RequireAuthorization();

        stations.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ReviewStationCommand(id, request.Approve, request.Reason), ct)).ToHttp())
            .WithSummary("Admin: approve or reject a submitted station")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
    }

    private static void MapBookings(RouteGroupBuilder bookings)
    {
        bookings.MapGet("/mine", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyBookingsQuery(), ct)).ToHttp());

        bookings.MapPost("/quote", async (GetQuoteQuery query, ISender sender, CancellationToken ct) =>
            (await sender.Send(query, ct)).ToHttp())
            .WithSummary("Price preview for a slot (same checks as booking)");

        bookings.MapPost("/", async (CreateBookingCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(b => TypedResults.Created($"/api/v1/bookings/{b.BookingId}", b)))
            .WithSummary("Book a connector for a time slot (409 if the slot was just taken)");

        bookings.MapPost("/{id:guid}/cancel", async (Guid id, CancelRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CancelBookingCommand(id, request.Reason), ct)).ToHttp());

        bookings.MapPost("/{id:guid}/respond", async (Guid id, RespondRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RespondToBookingCommand(id, request.Approve, request.Reason), ct)).ToHttp())
            .WithSummary("Host: accept or reject a booking on a RequiresApproval station");
    }

    private static void MapVehicles(RouteGroupBuilder vehicles)
    {
        vehicles.MapPost("/", async (AddVehicleCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttp(id => TypedResults.Created($"/api/v1/vehicles/{id}", new { id })));
    }
}
