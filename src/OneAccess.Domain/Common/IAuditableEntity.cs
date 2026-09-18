namespace OneAccess.Domain.Common;

/// <summary>
/// Contract for entities that track creation and optional modification timestamps in UTC.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp in UTC.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
