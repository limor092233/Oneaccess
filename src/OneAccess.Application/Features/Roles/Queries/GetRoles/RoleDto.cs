namespace OneAccess.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// DTO representing a security role with its assigned permission codes.
/// </summary>
public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAt
);
