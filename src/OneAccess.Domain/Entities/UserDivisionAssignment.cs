namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents an Administrator's management assignment over a specific Division.
/// Distinct from User.DivisionId, which is an ordinary user's home placement.
/// </summary>
public class UserDivisionAssignment
{
    /// <summary>
    /// Gets or sets the Administrator User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the managed Division identifier.
    /// </summary>
    public Guid DivisionId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when management was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Division Division { get; set; } = null!;
}
