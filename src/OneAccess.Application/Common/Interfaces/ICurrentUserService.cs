namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract providing access to the current authenticated caller's identity and permissions.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdministrator { get; }
    IReadOnlyList<string> Roles { get; }
    Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetAssignedDivisionIdsAsync(CancellationToken ct = default);
}
