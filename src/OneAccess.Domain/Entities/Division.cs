using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents an organizational division used to segment users and administrators.
/// </summary>
public class Division : BaseEntity
{
    /// <summary>
    /// Gets or sets the division name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the division description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<UserDivisionAssignment> UserDivisionAssignments { get; set; } = new List<UserDivisionAssignment>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
