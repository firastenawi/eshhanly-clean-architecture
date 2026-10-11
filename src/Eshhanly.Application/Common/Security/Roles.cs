namespace Eshhanly.Application.Common.Security;

/// <summary>Identity roles. A user can hold several (e.g. Guest + Host).</summary>
public static class Roles
{
    public const string Guest = "Guest";
    public const string Host = "Host";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Guest, Host, Admin];
}
