using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Response returned upon successful user creation.
/// </summary>
public record CreateUserResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    Guid? DivisionId,
    Guid? SectionId,
    UserStatus Status,
    DateTime CreatedAt
);
