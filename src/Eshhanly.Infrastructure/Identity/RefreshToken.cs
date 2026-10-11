namespace Eshhanly.Infrastructure.Identity;

/// <summary>
/// Rotating refresh token for API (mobile) clients. Only the SHA-256 hash is stored.
/// Reuse of a revoked token revokes the whole chain (token theft detection).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? DeviceInfo { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
