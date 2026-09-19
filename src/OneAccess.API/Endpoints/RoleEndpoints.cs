using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.API.Common;
using OneAccess.Application.Features.RolePermissions.Commands.AssignPermission;
using OneAccess.Application.Features.RolePermissions.Commands.RevokePermission;
using OneAccess.Application.Features.Roles.Commands.CreateRole;
using OneAccess.Application.Features.Roles.Commands.DeleteRole;
using OneAccess.Application.Features.Roles.Commands.UpdateRole;
using OneAccess.Application.Features.Roles.Queries.GetRoles;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Role and RolePermission management endpoints per OneAccess.md Section 14.
/// </summary>
public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles")
            .WithTags("Roles");

        // GET /api/roles - List all roles
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRolesQuery(), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("role.view")
        .WithName("GetRoles");

        // POST /api/roles - Create role
        group.MapPost("/", async ([FromBody] CreateRoleCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded)
            {
                return result.ToHttpResult();
            }
            return Results.Created($"/api/roles/{result.Value!.Id}", result.Value);
        })
        .RequirePermission("role.create")
        .WithName("CreateRole");

        // PUT /api/roles/{id} - Update role
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateRoleCommand(id, request.Name, request.Description);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("role.update")
        .WithName("UpdateRole");

        // DELETE /api/roles/{id} - Delete role
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRoleCommand(id), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("role.delete")
        .WithName("DeleteRole");

        // POST /api/roles/{id}/permissions - Assign permission to role
        group.MapPost("/{id:guid}/permissions", async (Guid id, [FromBody] AssignPermissionRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AssignPermissionCommand(id, request.PermissionId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("rolepermission.assign")
        .WithName("AssignRolePermission");

        // DELETE /api/roles/{id}/permissions/{permissionId} - Revoke permission from role
        group.MapDelete("/{id:guid}/permissions/{permissionId:guid}", async (Guid id, Guid permissionId, ISender sender, CancellationToken ct) =>
        {
            var command = new RevokePermissionCommand(id, permissionId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("rolepermission.revoke")
        .WithName("RevokeRolePermission");

        return app;
    }
}

public record UpdateRoleRequest(string Name, string Description);

public record AssignPermissionRequest(Guid PermissionId);
