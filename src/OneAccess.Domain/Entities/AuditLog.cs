using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents an immutable audit trail record for security-sensitive actions in OneAccess.
/// Stored in its own dedicated table.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the optional actor User identifier who performed the action.
    /// Null for anonymous/system actions (e.g. initial setup, failed login).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "login_success", "user_create", "role_assigned").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target entity type (e.g., "User", "Role", "Division", "SubSystem").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target entity identifier string.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional structured context serialized as JSON.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the originating client IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
