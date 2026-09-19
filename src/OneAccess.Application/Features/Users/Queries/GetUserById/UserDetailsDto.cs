using OneAccess.Domain.Enums;

namespace OneAccess.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Detailed DTO for user profile queries.
/// </summary>
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
