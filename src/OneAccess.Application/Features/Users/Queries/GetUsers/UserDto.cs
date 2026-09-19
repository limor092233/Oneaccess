using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// DTO representing a summary user item in list queries.
/// </summary>
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
