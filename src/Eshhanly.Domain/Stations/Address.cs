namespace Eshhanly.Domain.Stations;

/// <summary>Postal address value object. <see cref="Country"/> is ISO-3166 alpha-2 (default "SY").</summary>
public sealed record Address(string Line, string City, string Country = "SY")
{
    public const int LineMaxLength = 200;
    public const int CityMaxLength = 80;
}
