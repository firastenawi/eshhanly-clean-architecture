using Eshhanly.Application.Common;
using Eshhanly.Application.Common.Exceptions;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Bookings;
using Eshhanly.Domain.Common;
using Eshhanly.Domain.Stations;
using FluentValidation;
using MediatR;

namespace Eshhanly.Application.Bookings;

public sealed record CreateBookingCommand(
    Guid StationId,
    Guid ConnectorId,
    Guid? VehicleId,
    DateTimeOffset Start,
    DateTimeOffset End,
    PaymentProvider PaymentProvider = PaymentProvider.Simulated) : IRequest<Result<BookingCreatedResponse>>;

public sealed record BookingCreatedResponse(Guid BookingId, string Number, BookingStatus Status, long TotalMinor, string Currency);

internal sealed class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(c => c.End).GreaterThan(c => c.Start);
        RuleFor(c => c.PaymentProvider)
            .Must(p => p is PaymentProvider.Simulated or PaymentProvider.CashOnSite)
            .WithMessage("Only simulated and cash-on-site payments are available for now.");
    }
}

/// <summary>
/// Creates the booking and its payment in one transaction. Payments are simulated for now:
/// the hold succeeds immediately, so the booking moves straight to Confirmed (or AwaitingHostApproval).
/// </summary>
internal sealed class CreateBookingHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CreateBookingCommand, Result<BookingCreatedResponse>>
{
    public async Task<Result<BookingCreatedResponse>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } guestId)
        {
            return AppErrors.NotAuthenticated;
        }

        var now = clock.GetUtcNow();
        var prepared = await BookingPreparation.PrepareAsync(db, guestId, command.StationId, command.ConnectorId, command.VehicleId,
            command.Start, command.End, now, cancellationToken);
        if (prepared.IsFailure)
        {
            return prepared.Error;
        }

        var (station, connector, vehicle, slot, quote) = prepared.Value;

        var bookingResult = Booking.Create(guestId, station.Id, connector.Id, vehicle?.Id, slot, station.BufferMinutes, quote, now);
        if (bookingResult.IsFailure)
        {
            return bookingResult.Error;
        }

        var booking = bookingResult.Value;

        var payment = Payment.Create(booking.Id, command.PaymentProvider, quote.Currency);
        payment.Authorize(quote.TotalMinor, $"SIM-{booking.Number}", booking.EndAt.AddDays(1), $"{booking.Id}:authorize", now);
        booking.MarkPaymentHeld(station.BookingMode == BookingMode.RequiresApproval, now);

        db.Bookings.Add(booking);
        db.Payments.Add(payment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (SlotUnavailableException)
        {
            return BookingErrors.SlotTaken; // lost the race to a concurrent booking
        }

        return new BookingCreatedResponse(booking.Id, booking.Number, booking.Status, booking.EstimatedTotalMinor, booking.Currency);
    }
}
