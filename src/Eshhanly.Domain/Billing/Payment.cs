using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Billing;

public enum PaymentProvider
{
    Simulated = 0,
    /// <summary>Guest pays the host in cash at the station; the platform commission becomes a host debt.</summary>
    CashOnSite = 1,
    /// <summary>Local mobile wallet (Sham Cash, Syriatel Cash, ...) once an API is integrated.</summary>
    Wallet = 2,
}

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    PartiallyRefunded = 3,
    Refunded = 4,
    Voided = 5,
    Failed = 6,
}

public enum PaymentOperation
{
    Authorize = 0,
    Capture = 1,
    Void = 2,
    Refund = 3,
}

public sealed record PaymentCaptured(Guid PaymentId, Guid BookingId, long AmountMinor, string Currency, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record PaymentRefunded(Guid PaymentId, Guid BookingId, long AmountMinor, string Currency, DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>Money flow of one booking: hold (authorize) → capture actual amount → optional refunds; or void.</summary>
public sealed class Payment : AggregateRoot
{
    private readonly List<PaymentTransaction> _transactions = [];

    private Payment() { }

    public Guid BookingId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? ProviderPaymentId { get; private set; }
    public long AmountHeldMinor { get; private set; }
    public long AmountCapturedMinor { get; private set; }
    public long AmountRefundedMinor { get; private set; }
    public DateTimeOffset? HoldExpiresAt { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactions.AsReadOnly();

    public static Payment Create(Guid bookingId, PaymentProvider provider, string currency) => new()
    {
        BookingId = bookingId,
        Provider = provider,
        Currency = Currencies.Normalize(currency),
        Status = PaymentStatus.Pending,
    };

    public Result Authorize(long amountMinor, string? providerPaymentId, DateTimeOffset holdExpiresAt, string idempotencyKey, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            return InvalidTransition("authorize");
        }

        if (amountMinor <= 0)
        {
            return Error.Validation("Payment.InvalidAmount", "Hold amount must be greater than zero.");
        }

        Status = PaymentStatus.Authorized;
        AmountHeldMinor = amountMinor;
        ProviderPaymentId = providerPaymentId;
        HoldExpiresAt = holdExpiresAt;
        Record(PaymentOperation.Authorize, amountMinor, providerPaymentId, idempotencyKey, now);
        return Result.Success();
    }

    /// <summary>Captures the actual amount (≤ held). Cash-on-site captures whatever the host confirms receiving.</summary>
    public Result Capture(long amountMinor, string? providerReference, string idempotencyKey, DateTimeOffset now)
    {
        if (Status != PaymentStatus.Authorized)
        {
            return InvalidTransition("capture");
        }

        if (amountMinor < 0 || (Provider != PaymentProvider.CashOnSite && amountMinor > AmountHeldMinor))
        {
            return Error.Validation("Payment.CaptureExceedsHold", "Captured amount cannot exceed the held amount.");
        }

        Status = PaymentStatus.Captured;
        AmountCapturedMinor = amountMinor;
        HoldExpiresAt = null;
        Record(PaymentOperation.Capture, amountMinor, providerReference, idempotencyKey, now);
        Raise(new PaymentCaptured(Id, BookingId, amountMinor, Currency, now));
        return Result.Success();
    }

    public Result Void(string idempotencyKey, DateTimeOffset now)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return InvalidTransition("void");
        }

        Status = PaymentStatus.Voided;
        HoldExpiresAt = null;
        Record(PaymentOperation.Void, AmountHeldMinor, null, idempotencyKey, now);
        return Result.Success();
    }

    public Result Refund(long amountMinor, string? providerReference, string idempotencyKey, DateTimeOffset now)
    {
        if (Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
        {
            return InvalidTransition("refund");
        }

        if (amountMinor <= 0 || AmountRefundedMinor + amountMinor > AmountCapturedMinor)
        {
            return Error.Validation("Payment.RefundExceedsCapture", "Refund total cannot exceed the captured amount.");
        }

        AmountRefundedMinor += amountMinor;
        Status = AmountRefundedMinor == AmountCapturedMinor ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        Record(PaymentOperation.Refund, amountMinor, providerReference, idempotencyKey, now);
        Raise(new PaymentRefunded(Id, BookingId, amountMinor, Currency, now));
        return Result.Success();
    }

    public void Fail(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
    }

    private void Record(PaymentOperation operation, long amountMinor, string? providerReference, string idempotencyKey, DateTimeOffset now) =>
        _transactions.Add(new PaymentTransaction(Id, operation, amountMinor, providerReference, idempotencyKey, now));

    private Error InvalidTransition(string action) =>
        Error.Conflict("Payment.InvalidTransition", $"Cannot {action} a payment in status {Status}.");
}

/// <summary>Audit row for each gateway call. <see cref="IdempotencyKey"/> is unique so retried requests never double-charge.</summary>
public sealed class PaymentTransaction : Entity
{
    public const int IdempotencyKeyMaxLength = 64;

    private PaymentTransaction() { }

    internal PaymentTransaction(Guid paymentId, PaymentOperation operation, long amountMinor, string? providerReference, string idempotencyKey, DateTimeOffset occurredAt)
    {
        PaymentId = paymentId;
        Operation = operation;
        AmountMinor = amountMinor;
        ProviderReference = providerReference;
        IdempotencyKey = idempotencyKey;
        OccurredAt = occurredAt;
    }

    public Guid PaymentId { get; private set; }
    public PaymentOperation Operation { get; private set; }
    public long AmountMinor { get; private set; }
    public string? ProviderReference { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}
