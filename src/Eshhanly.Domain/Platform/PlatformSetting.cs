namespace Eshhanly.Domain.Platform;

/// <summary>Admin-editable key/value setting (commission rate, grace periods, ...). Values are strings parsed by the reader.</summary>
public sealed class PlatformSetting
{
    public const int KeyMaxLength = 100;
    public const int ValueMaxLength = 500;

    private PlatformSetting() { }

    public PlatformSetting(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string value, DateTimeOffset now)
    {
        Value = value;
        UpdatedAt = now;
    }
}

public static class PlatformSettingKeys
{
    public const string CommissionRate = "Billing.CommissionRate";
    public const string DefaultCurrency = "Billing.DefaultCurrency";
}
