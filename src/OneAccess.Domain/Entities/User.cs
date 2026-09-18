using OneAccess.Domain.Common;
using OneAccess.Domain.Enums;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a user account in the OneAccess identity system.
/// </summary>
public class User : BaseEntity, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the unique username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt password hash.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display / full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account status (Active, Inactive, Suspended).
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// Gets or sets a value indicating whether this user is the root System Administrator.
    /// </summary>
    public bool IsSystemAdministrator { get; set; }

    /// <summary>
    /// Gets or sets the optional home division placement.
    /// </summary>
    public Guid? DivisionId { get; set; }

    /// <summary>
    /// Gets or sets the optional home section placement under the division.
    /// </summary>
    public Guid? SectionId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Division? Division { get; set; }
    public Section? Section { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserSubSystemAccess> UserSubSystemAccesses { get; set; } = new List<UserSubSystemAccess>();
    public ICollection<UserDivisionAssignment> UserDivisionAssignments { get; set; } = new List<UserDivisionAssignment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
