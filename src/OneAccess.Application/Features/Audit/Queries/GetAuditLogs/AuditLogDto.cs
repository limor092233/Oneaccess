namespace OneAccess.Application.Features.Audit.Queries.GetAuditLogs;

/// <summary>
/// Data transfer object representing an audit log entry.
/// </summary>
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
