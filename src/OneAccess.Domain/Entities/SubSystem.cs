using OneAccess.Domain.Common;

namespace OneAccess.Domain.Entities;

/// <summary>
/// Represents a registered external sub-system that relies on OneAccess for single sign-on authentication.
/// </summary>
public class SubSystem : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique short code for the sub-system (e.g. "SYS1").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the sub-system.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL where the sub-system is hosted.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected JWT audience claim value (e.g. "system1.api").
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this sub-system is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserSubSystemAccess> UserSubSystemAccesses { get; set; } = new List<UserSubSystemAccess>();
    public ICollection<RoleSubSystemAccess> RoleSubSystemAccesses { get; set; } = new List<RoleSubSystemAccess>();
}
