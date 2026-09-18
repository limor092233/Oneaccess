namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents the assignment of a Permission to a Role.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Gets or sets the Role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the Permission identifier.
    /// </summary>
    public Guid PermissionId { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
