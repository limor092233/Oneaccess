using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.API.Common;
using OneAccess.Application.Features.Audit.Queries.GetAuditLogs;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Audit trail querying endpoints per OneAccess.md Section 12 & Section 14.
/// Gated by audit.view (non-delegable system permission).
/// </summary>
public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit");

        // GET /api/audit - List and filter audit logs
        group.MapGet("/", async (
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null,
            ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var query = new GetAuditLogsQuery(pageNumber, pageSize, userId, entityType, action, fromUtc, toUtc);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("audit.view")
        .WithName("GetAuditLogs");

        return app;
    }
}
