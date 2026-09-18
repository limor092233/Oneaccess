namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a role-level sub-system restriction allow-list entry.
/// Meaningful only for Administrator and User roles (not System Administrator).
/// </summary>
public class RoleSubSystemAccess
{
    /// <summary>
    /// Gets or sets the Role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the SubSystem identifier.
    /// </summary>
    public Guid SubSystemId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this restriction was added.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Role Role { get; set; } = null!;
    public SubSystem SubSystem { get; set; } = null!;
}
