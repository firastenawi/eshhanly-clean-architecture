namespace Eshhanly.Domain.Common;

/// <summary>Small helpers shared by aggregates to turn invariant checks into <see cref="Error"/>s.</summary>
internal static class Guard
{
    public static Error? Required(string? value, string field, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? Error.Validation($"{field}.Required", $"{field} is required.")
            : value.Trim().Length > maxLength
                ? Error.Validation($"{field}.TooLong", $"{field} must be at most {maxLength} characters.")
                : null;

    public static Error? Positive(decimal value, string field) =>
        value <= 0 ? Error.Validation($"{field}.NotPositive", $"{field} must be greater than zero.") : null;
}
