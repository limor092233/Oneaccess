namespace OneAccess.Client.Models;

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAt
);

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public record AssignPermissionRequest(Guid PermissionId);
