using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Audit.Queries.GetAuditLogs;

/// <summary>
/// Query to retrieve paginated and filtered audit log entries.
/// Deliberately does NOT implement IDivisionScopedRequest (OneAccess.md Section 5).
/// Gated by audit.view (non-delegable system permission).
/// </summary>
public record GetAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? EntityType = null,
    string? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null
) : IRequest<Result<PagedResult<AuditLogDto>>>;
