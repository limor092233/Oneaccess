namespace OneAccess.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Response returned upon successful role update.
/// </summary>
public record UpdateRoleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole
);
