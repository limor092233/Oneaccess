namespace OneAccess.Client.Models;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    UserStatus Status,
    bool IsSystemAdministrator,
    Guid? DivisionId,
    Guid? SectionId,
    DateTime CreatedAt
);

public record UserDetailsDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    UserStatus Status,
    bool IsSystemAdministrator,
    Guid? DivisionId,
    Guid? SectionId,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? DivisionId { get; set; }
    public Guid? SectionId { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public record CreateUserResponse(Guid Id, string Username, string Email);

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? DivisionId { get; set; }
    public Guid? SectionId { get; set; }
    public UserStatus? Status { get; set; } = UserStatus.Active;
}

public record AssignRoleRequest(Guid RoleId);
