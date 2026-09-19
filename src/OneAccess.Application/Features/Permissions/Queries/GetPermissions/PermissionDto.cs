namespace OneAccess.Application.Features.Permissions.Queries.GetPermissions;

/// <summary>
/// Data transfer object for a system permission.
/// Includes IsDelegable so UI can show non-delegable permissions as disabled/unassignable.
/// </summary>
public record PermissionDto(
    Guid Id,
    string Code,
    string Module,
    string Description,
    bool IsDelegable
);
