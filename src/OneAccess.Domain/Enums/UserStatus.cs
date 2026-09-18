namespace OneAccess.Domain.Enums;

/// <summary>
/// Status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is active and able to authenticate.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User account is inactive / soft-deleted.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// User account is suspended.
    /// </summary>
    Suspended = 3
}
