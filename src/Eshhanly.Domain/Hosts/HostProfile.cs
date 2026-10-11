using Eshhanly.Domain.Common;

namespace Eshhanly.Domain.Hosts;

public enum KycStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2,
}

/// <summary>Host-side profile of a user. A user becomes a host by creating one (1:1 with the identity user).</summary>
public sealed class HostProfile : AggregateRoot
{
    public const int DisplayNameMaxLength = 100;

    private HostProfile() { }

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public KycStatus KycStatus { get; private set; }

    /// <summary>Overrides the platform commission (0..1) for this host, e.g. for early partners.</summary>
    public decimal? CommissionRateOverride { get; private set; }

    /// <summary>Opaque reference to payout details (wallet number / bank ref) stored encrypted; never raw card data.</summary>
    public string? PayoutReference { get; private set; }

    public decimal RatingAverage { get; private set; }
    public int RatingCount { get; private set; }

    public static Result<HostProfile> Create(Guid userId, string displayName)
    {
        if (Guard.Required(displayName, nameof(DisplayName), DisplayNameMaxLength) is { } error)
        {
            return error;
        }

        return new HostProfile
        {
            UserId = userId,
            DisplayName = displayName.Trim(),
            KycStatus = KycStatus.Pending,
        };
    }

    public void Verify() => KycStatus = KycStatus.Verified;

    public void RejectKyc() => KycStatus = KycStatus.Rejected;

    public void SetPayoutReference(string? reference) => PayoutReference = reference?.Trim();

    public Result SetCommissionOverride(decimal? rate)
    {
        if (rate is < 0 or > 1)
        {
            return Error.Validation("Host.CommissionOutOfRange", "Commission rate must be between 0 and 1.");
        }

        CommissionRateOverride = rate;
        return Result.Success();
    }

    public void ApplyRating(int rating)
    {
        RatingAverage = Math.Round(((RatingAverage * RatingCount) + rating) / (RatingCount + 1), 2);
        RatingCount++;
    }
}
