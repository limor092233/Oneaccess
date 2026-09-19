namespace OneAccess.Client.Models;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityType,
    string EntityId,
    string Details,
    string? IpAddress,
    DateTime CreatedAt
);
