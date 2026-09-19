namespace OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;

/// <summary>
/// Response returned after updating a sub-system.
/// </summary>
public record UpdateSubSystemResponse(
    Guid Id,
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
);
