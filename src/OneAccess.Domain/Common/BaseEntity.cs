namespace OneAccess.Domain.Common;

/// <summary>
/// Base class for domain entities with a Guid identifier.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
}
