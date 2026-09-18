using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a one-time cryptographic code generated at startup for first-run System Administrator initialization.
/// Stored hashed and consumed upon use.
/// </summary>
public class SetupCode : BaseEntity
{
    /// <summary>
    /// Gets or sets the hashed setup code. Plaintext is only emitted to the console.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this setup code was consumed. Null if unconsumed.
    /// </summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether this setup code is still valid.
    /// </summary>
    public bool IsValid => ConsumedAt == null && DateTime.UtcNow < ExpiresAt;
}
