namespace OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;

/// <summary>
/// Response payload returned after registering a sub-system.
/// </summary>
public record RegisterSubSystemResponse(
    Guid Id,
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
);
