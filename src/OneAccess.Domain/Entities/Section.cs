using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a sub-unit / section within an organizational division.
/// </summary>
public class Section : BaseEntity
{
    /// <summary>
    /// Gets or sets the parent Division identifier.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Division Division { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
}
