using Eshhanly.Domain.Billing;
using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Tests.Billing;

public class BillingTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Capture_posting_splits_commission_and_balances()
    {
        var entries = Ledger.PostCapture(Guid.NewGuid(), Guid.NewGuid(), new Money(10_000, "SYP"), 0.15m, Now);

        entries.Sum(e => e.DebitMinor).ShouldBe(entries.Sum(e => e.CreditMinor));
        entries.Single(e => e.Account == LedgerAccount.PlatformRevenue).CreditMinor.ShouldBe(1_500);
        entries.Single(e => e.Account == LedgerAccount.HostPayable).CreditMinor.ShouldBe(8_500);
    }

    [Fact]
    public void Cash_posting_books_commission_as_host_debt()
    {
        var entries = Ledger.PostCashCollected(Guid.NewGuid(), Guid.NewGuid(), new Money(10_000, "SYP"), 0.15m, Now);

        entries.Single(e => e.Account == LedgerAccount.HostPayable).DebitMinor.ShouldBe(1_500);
        entries.Single(e => e.Account == LedgerAccount.PlatformRevenue).CreditMinor.ShouldBe(1_500);
    }

    [Fact]
    public void Payment_hold_capture_refund_flow()
    {
        var payment = Payment.Create(Guid.NewGuid(), PaymentProvider.Simulated, "syp");

        payment.Authorize(10_000, "sim_1", Now.AddDays(7), "k1", Now).IsSuccess.ShouldBeTrue();
        payment.Capture(12_000, null, "k2", Now).IsFailure.ShouldBeTrue(); // more than held
        payment.Capture(8_000, null, "k2", Now).IsSuccess.ShouldBeTrue();
        payment.Refund(3_000, null, "k3", Now).IsSuccess.ShouldBeTrue();

        payment.Status.ShouldBe(PaymentStatus.PartiallyRefunded);
        payment.Currency.ShouldBe("SYP");
        payment.Transactions.Count.ShouldBe(3);
        payment.Refund(6_000, null, "k4", Now).IsFailure.ShouldBeTrue(); // beyond captured
    }

    [Fact]
    public void Cash_on_site_may_capture_more_than_estimate()
    {
        var payment = Payment.Create(Guid.NewGuid(), PaymentProvider.CashOnSite, "SYP");
        payment.Authorize(10_000, null, Now.AddDays(1), "k1", Now);

        payment.Capture(11_000, null, "k2", Now).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Payout_disbursement_returns_balanced_ledger_rows()
    {
        var payout = Payout.Schedule(Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7), new Money(50_000, "SYP")).Value;

        var entries = payout.MarkDisbursed("manual-001", Now).Value;

        payout.Status.ShouldBe(PayoutStatus.Disbursed);
        entries.Sum(e => e.DebitMinor).ShouldBe(entries.Sum(e => e.CreditMinor));
        entries.ShouldAllBe(e => e.PayoutId == payout.Id);
    }

    [Fact]
    public void Money_refuses_mixed_currencies()
    {
        Should.Throw<InvalidOperationException>(() => new Money(1, "SYP") + new Money(1, "USD"));
    }
}
