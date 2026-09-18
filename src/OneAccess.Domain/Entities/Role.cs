using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a security role containing permissions.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique role name (e.g., "System Administrator", "Administrator", "User").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a protected system role that cannot be deleted or modified by non-SysAdmins.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<RoleSubSystemAccess> RoleSubSystemAccesses { get; set; } = new List<RoleSubSystemAccess>();
}
