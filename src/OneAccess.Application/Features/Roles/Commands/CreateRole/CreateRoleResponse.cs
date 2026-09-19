namespace OneAccess.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Response returned upon successful role creation.
/// </summary>
public record CreateRoleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    DateTime CreatedAt
);
