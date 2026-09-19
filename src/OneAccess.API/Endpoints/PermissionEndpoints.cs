using MediatR;
using OneAccess.API.Authorization;
using OneAccess.API.Common;
using OneAccess.Application.Features.Permissions.Queries.GetPermissions;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Permission listing endpoints per OneAccess.md Section 14.
/// </summary>
public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/permissions")
            .WithTags("Permissions");

        // GET /api/permissions - List all seeded permissions
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPermissionsQuery(), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("permission.view")
        .WithName("GetPermissions");

        return app;
    }
}
