namespace OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;

/// <summary>
/// Data transfer object representing a sub-system.
/// </summary>
public record SubSystemDto(
    Guid Id,
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
);
