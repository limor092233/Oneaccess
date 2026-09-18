using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a granular permission code granting capability in OneAccess.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique permission code (e.g. "user.create", "role.view").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module / functional area this permission belongs to (e.g. "Users", "Roles", "Divisions").
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description of what this permission permits.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
