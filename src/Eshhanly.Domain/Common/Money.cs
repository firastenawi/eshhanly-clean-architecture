namespace Eshhanly.Domain.Common;

/// <summary>
/// Amount in the currency's minor unit (SYP has none in practice, so 1 = 1 pound) plus ISO-4217 code.
/// Persisted as two columns (<c>*_minor bigint</c> + <c>currency char(3)</c>), never as floating point.
/// </summary>
public readonly record struct Money
{
    public Money(long amountMinor, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amountMinor);
        Currency = Currencies.Normalize(currency);
        AmountMinor = amountMinor;
    }

    public long AmountMinor { get; }
    public string Currency { get; }

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(checked(AmountMinor + other.AmountMinor), Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(AmountMinor - other.AmountMinor, Currency);
    }

    /// <summary>Multiplies by a rate (e.g. a commission of 0.15), rounding half away from zero.</summary>
    public Money Multiply(decimal factor) =>
        new((long)Math.Round(AmountMinor * factor, MidpointRounding.AwayFromZero), Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
        }
    }

    public override string ToString() => $"{AmountMinor} {Currency}";
}

public static class Currencies
{
    public const string SyrianPound = "SYP";
    public const string UsDollar = "USD";

    public static string Normalize(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO-4217 code.", nameof(currency));
        }

        return currency.Trim().ToUpperInvariant();
    }
}
