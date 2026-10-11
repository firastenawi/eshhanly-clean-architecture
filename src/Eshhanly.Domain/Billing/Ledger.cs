using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Billing;

public enum LedgerAccount
{
    /// <summary>Money collected from guests, waiting to be split.</summary>
    GuestClearing = 0,
    /// <summary>Platform commission earned.</summary>
    PlatformRevenue = 1,
    /// <summary>What the platform owes a host (credit) or a host owes the platform (debit, e.g. cash commission).</summary>
    HostPayable = 2,
    /// <summary>Money returned to guests.</summary>
    Refunds = 3,
    /// <summary>Money paid out to hosts.</summary>
    HostDisbursements = 4,
}

/// <summary>Append-only double-entry row. Each <see cref="TransactionGroupId"/> balances (Σ debit = Σ credit).</summary>
public sealed class LedgerEntry
{
    public const int DescriptionMaxLength = 200;

    private LedgerEntry() { }

    internal LedgerEntry(Guid groupId, LedgerAccount account, long debitMinor, long creditMinor, string currency, string description, DateTimeOffset now)
    {
        TransactionGroupId = groupId;
        Account = account;
        DebitMinor = debitMinor;
        CreditMinor = creditMinor;
        Currency = currency;
        Description = description;
        CreatedAt = now;
    }

    public long Id { get; private set; }
    public Guid TransactionGroupId { get; private set; }
    public LedgerAccount Account { get; private set; }
    public Guid? HostId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public Guid? PayoutId { get; private set; }
    public long DebitMinor { get; private set; }
    public long CreditMinor { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    internal LedgerEntry For(Guid? hostId, Guid? paymentId, Guid? payoutId)
    {
        HostId = hostId;
        PaymentId = paymentId;
        PayoutId = payoutId;
        return this;
    }
}

/// <summary>Builds balanced ledger postings for each money event. The only way to create <see cref="LedgerEntry"/> rows.</summary>
public static class Ledger
{
    /// <summary>Online capture: guest money is split into platform commission and host earnings.</summary>
    public static IReadOnlyList<LedgerEntry> PostCapture(Guid paymentId, Guid hostId, Money gross, decimal commissionRate, DateTimeOffset now)
    {
        var commission = gross.Multiply(commissionRate);
        var hostNet = gross - commission;
        var group = Guid.CreateVersion7();

        return Balanced(
        [
            new LedgerEntry(group, LedgerAccount.GuestClearing, gross.AmountMinor, 0, gross.Currency, "Guest payment captured", now).For(hostId, paymentId, null),
            new LedgerEntry(group, LedgerAccount.PlatformRevenue, 0, commission.AmountMinor, gross.Currency, "Platform commission", now).For(hostId, paymentId, null),
            new LedgerEntry(group, LedgerAccount.HostPayable, 0, hostNet.AmountMinor, gross.Currency, "Host earnings", now).For(hostId, paymentId, null),
        ]);
    }

    /// <summary>Cash on site: the host already holds the full amount, so only the commission is booked as host debt.</summary>
    public static IReadOnlyList<LedgerEntry> PostCashCollected(Guid paymentId, Guid hostId, Money gross, decimal commissionRate, DateTimeOffset now)
    {
        var commission = gross.Multiply(commissionRate);
        var group = Guid.CreateVersion7();

        return Balanced(
        [
            new LedgerEntry(group, LedgerAccount.HostPayable, commission.AmountMinor, 0, gross.Currency, "Commission owed on cash booking", now).For(hostId, paymentId, null),
            new LedgerEntry(group, LedgerAccount.PlatformRevenue, 0, commission.AmountMinor, gross.Currency, "Platform commission (cash)", now).For(hostId, paymentId, null),
        ]);
    }

    /// <summary>Refund reverses the split proportionally: host and platform each give back their share.</summary>
    public static IReadOnlyList<LedgerEntry> PostRefund(Guid paymentId, Guid hostId, Money refund, decimal commissionRate, DateTimeOffset now)
    {
        var commissionPart = refund.Multiply(commissionRate);
        var hostPart = refund - commissionPart;
        var group = Guid.CreateVersion7();

        return Balanced(
        [
            new LedgerEntry(group, LedgerAccount.PlatformRevenue, commissionPart.AmountMinor, 0, refund.Currency, "Commission reversed", now).For(hostId, paymentId, null),
            new LedgerEntry(group, LedgerAccount.HostPayable, hostPart.AmountMinor, 0, refund.Currency, "Host earnings reversed", now).For(hostId, paymentId, null),
            new LedgerEntry(group, LedgerAccount.Refunds, 0, refund.AmountMinor, refund.Currency, "Refund to guest", now).For(hostId, paymentId, null),
        ]);
    }

    public static IReadOnlyList<LedgerEntry> PostPayout(Guid payoutId, Guid hostId, Money amount, DateTimeOffset now)
    {
        var group = Guid.CreateVersion7();

        return Balanced(
        [
            new LedgerEntry(group, LedgerAccount.HostPayable, amount.AmountMinor, 0, amount.Currency, "Payout to host", now).For(hostId, null, payoutId),
            new LedgerEntry(group, LedgerAccount.HostDisbursements, 0, amount.AmountMinor, amount.Currency, "Payout disbursed", now).For(hostId, null, payoutId),
        ]);
    }

    private static LedgerEntry[] Balanced(LedgerEntry[] entries)
    {
        if (entries.Sum(e => e.DebitMinor) != entries.Sum(e => e.CreditMinor))
        {
            throw new InvalidOperationException("Ledger posting is not balanced.");
        }

        return entries;
    }
}
