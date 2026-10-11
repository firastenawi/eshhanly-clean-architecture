using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Billing;

public enum PayoutStatus
{
    Scheduled = 0,
    Processing = 1,
    Disbursed = 2,
    Failed = 3,
}

/// <summary>A transfer of accumulated <see cref="LedgerAccount.HostPayable"/> balance to a host (manual at launch).</summary>
public sealed class Payout : AggregateRoot
{
    private Payout() { }

    public Guid HostId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PayoutStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? DisbursedAt { get; private set; }

    public static Result<Payout> Schedule(Guid hostId, DateOnly periodStart, DateOnly periodEnd, Money amount)
    {
        if (periodEnd < periodStart)
        {
            return Error.Validation("Payout.InvalidPeriod", "Payout period end must not be before its start.");
        }

        if (amount.AmountMinor <= 0)
        {
            return Error.Validation("Payout.InvalidAmount", "Payout amount must be greater than zero.");
        }

        return new Payout
        {
            HostId = hostId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AmountMinor = amount.AmountMinor,
            Currency = amount.Currency,
            Status = PayoutStatus.Scheduled,
        };
    }

    public Result MarkProcessing() => Move(PayoutStatus.Scheduled, PayoutStatus.Processing);

    /// <summary>Marks the payout as sent; returns the ledger rows the caller must persist in the same transaction.</summary>
    public Result<IReadOnlyList<LedgerEntry>> MarkDisbursed(string providerReference, DateTimeOffset now)
    {
        if (Status is not (PayoutStatus.Scheduled or PayoutStatus.Processing))
        {
            return Error.Conflict("Payout.InvalidTransition", $"Cannot disburse a payout in status {Status}.");
        }

        Status = PayoutStatus.Disbursed;
        ProviderReference = providerReference;
        DisbursedAt = now;
        return Result.Success(Ledger.PostPayout(Id, HostId, new Money(AmountMinor, Currency), now));
    }

    public Result MarkFailed(string reason)
    {
        if (Status == PayoutStatus.Disbursed)
        {
            return Error.Conflict("Payout.InvalidTransition", "A disbursed payout cannot fail.");
        }

        Status = PayoutStatus.Failed;
        FailureReason = reason;
        return Result.Success();
    }

    private Result Move(PayoutStatus from, PayoutStatus to)
    {
        if (Status != from)
        {
            return Error.Conflict("Payout.InvalidTransition", $"Cannot move a payout from {Status} to {to}.");
        }

        Status = to;
        return Result.Success();
    }
}
