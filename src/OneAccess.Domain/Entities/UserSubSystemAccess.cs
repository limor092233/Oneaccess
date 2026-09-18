namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents an explicit per-user sub-system access override grant.
/// When any rows exist for a user, they define exclusively the sub-systems the user can access.
/// </summary>
public class UserSubSystemAccess
{
    /// <summary>
    /// Gets or sets the User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the SubSystem identifier.
    /// </summary>
    public Guid SubSystemId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this access was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public SubSystem SubSystem { get; set; } = null!;
}
