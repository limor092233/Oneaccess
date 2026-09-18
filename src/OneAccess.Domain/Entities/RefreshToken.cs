using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a hashed refresh token issued to a user session for rotating access tokens.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the owning User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the SHA256/hash of the refresh token. Plaintext is never stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this token was revoked. Null if active.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the token that replaced this token upon rotation.
    /// Used for refresh token theft/reuse detection.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
