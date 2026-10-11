using Eshhanly.Domain.Bookings;

namespace Eshhanly.Domain.Tests.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Create_starts_pending_with_payment_deadline_and_buffer()
    {
        var booking = TestData.NewBooking();

        booking.Status.ShouldBe(BookingStatus.Pending);
        booking.Number.ShouldStartWith("ESH-");
        booking.Number.Length.ShouldBe(Booking.NumberLength);
        booking.ExpiresAt.ShouldBe(Now + Booking.PaymentHoldWindow);
        booking.BlockedUntil.ShouldBe(booking.EndAt.AddMinutes(15));
        booking.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<BookingCreated>();
    }

    [Fact]
    public void Create_rejects_slot_in_the_past()
    {
        var quote = new PriceQuote { Currency = "SYP" };
        var result = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, TestData.Slot(-1, 2), 0, quote, Now);

        result.Error.ShouldBe(BookingErrors.StartInPast);
    }

    [Fact]
    public void Instant_booking_is_confirmed_once_payment_is_held()
    {
        var booking = TestData.NewBooking();

        booking.MarkPaymentHeld(requiresHostApproval: false, Now).IsSuccess.ShouldBeTrue();

        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void Approval_booking_waits_for_host_then_confirms()
    {
        var booking = TestData.NewBooking(startHoursFromNow: 24);

        booking.MarkPaymentHeld(requiresHostApproval: true, Now);
        booking.Status.ShouldBe(BookingStatus.AwaitingHostApproval);
        booking.ExpiresAt.ShouldBe(Now + Booking.HostApprovalWindow);

        booking.ApproveByHost(Now.AddMinutes(10)).IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Confirmed);
    }

    [Fact]
    public void Host_rejection_cancels_booking()
    {
        var booking = TestData.NewBooking(startHoursFromNow: 24);
        booking.MarkPaymentHeld(true, Now);

        booking.RejectByHost("Car too big", Now).IsSuccess.ShouldBeTrue();

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancelledBy.ShouldBe(BookingParty.Host);
    }

    [Fact]
    public void Full_happy_path_reaches_completed()
    {
        var booking = TestData.NewBooking();
        booking.MarkPaymentHeld(false, Now);

        booking.Start(booking.StartAt.AddMinutes(-10)).IsSuccess.ShouldBeTrue();
        booking.Complete(9_500, booking.EndAt).IsSuccess.ShouldBeTrue();

        booking.Status.ShouldBe(BookingStatus.Completed);
        booking.FinalTotalMinor.ShouldBe(9_500);
    }

    [Fact]
    public void Cannot_start_earlier_than_allowance()
    {
        var booking = TestData.NewBooking();
        booking.MarkPaymentHeld(false, Now);

        booking.Start(booking.StartAt.AddMinutes(-30)).Error.ShouldBe(BookingErrors.TooEarlyToStart);
    }

    [Fact]
    public void Expire_only_after_deadline()
    {
        var booking = TestData.NewBooking();

        booking.Expire(Now.AddMinutes(5)).Error.ShouldBe(BookingErrors.NotExpiredYet);
        booking.Expire(Now.AddMinutes(16)).IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Expired);
    }

    [Fact]
    public void No_show_only_after_grace_period()
    {
        var booking = TestData.NewBooking();
        booking.MarkPaymentHeld(false, Now);

        booking.MarkNoShow(booking.StartAt.AddMinutes(10)).Error.ShouldBe(BookingErrors.GracePeriodNotOver);
        booking.MarkNoShow(booking.StartAt.AddMinutes(31)).IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.NoShow);
    }

    [Fact]
    public void Completed_booking_cannot_be_cancelled()
    {
        var booking = TestData.NewBooking();
        booking.MarkPaymentHeld(false, Now);
        booking.Start(booking.StartAt);
        booking.Complete(10_000, booking.EndAt);

        booking.Cancel(BookingParty.Guest, null, booking.EndAt).IsFailure.ShouldBeTrue();
    }
}
