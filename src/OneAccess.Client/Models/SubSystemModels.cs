namespace OneAccess.Client.Models;

public record SubSystemDto(
    Guid Id,
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
);

public record CreateSubSystemRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public record UpdateSubSystemRequest
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public record RegisterSubSystemResponse(
    Guid Id,
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
);

public record AssignRoleSubSystemRequest(Guid SubSystemId);

public record AssignUserSubSystemRequest(Guid SubSystemId);
