using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Response returned upon successful user update.
/// </summary>
public record UpdateUserResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    Guid? DivisionId,
    Guid? SectionId,
    UserStatus Status,
    DateTime? UpdatedAt
);
