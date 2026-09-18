namespace OneAccess.Application.Features.Auth.Queries.GetCurrentUser;

public record CurrentSubsystemInfo(Guid Id, string Code, string Name, string BaseUrl);

public record CurrentUserResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsSystemAdministrator,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<CurrentSubsystemInfo> SubSystems);
