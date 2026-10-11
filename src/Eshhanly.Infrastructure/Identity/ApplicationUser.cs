using Microsoft.AspNetCore.Identity;

namespace Eshhanly.Infrastructure.Identity;

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
}

/// <summary>
/// Identity account (email/phone, password hash, 2FA, roles). Lives in Infrastructure so the Domain stays
/// framework-free; domain aggregates reference users by <see cref="Guid"/> only.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public const int FullNameMaxLength = 150;

    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public string FullName { get; set; } = string.Empty;

    /// <summary>"ar" or "en".</summary>
    public string PreferredLanguage { get; set; } = "ar";

    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() => Id = Guid.CreateVersion7();

    public ApplicationRole(string roleName) : this()
    {
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
    }
}
