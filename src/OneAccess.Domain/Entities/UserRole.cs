namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between a User and a Role.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets the User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the Role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
