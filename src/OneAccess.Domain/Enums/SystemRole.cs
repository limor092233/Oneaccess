namespace OneAccess.Domain.Enums;

/// <summary>
/// Built-in system roles in OneAccess.
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// System Administrator with unrestricted full control across all features and divisions.
    /// </summary>
    SystemAdministrator = 1,

    /// <summary>
    /// Administrator scoped to assigned divisions.
    /// </summary>
    Administrator = 2,

    /// <summary>
    /// Standard user with access to assigned or default sub-systems.
    /// </summary>
    User = 3
}

/// <summary>
/// Seeded role name constants.
/// </summary>
public static class SystemRoles
{
    /// <summary>
    /// System Administrator role name.
    /// </summary>
    public const string SystemAdministrator = "System Administrator";

    /// <summary>
    /// Administrator role name.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// User role name.
    /// </summary>
    public const string User = "User";
}
