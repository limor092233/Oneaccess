using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.API.Common;
using OneAccess.Application.Features.SubSystems.Commands.AssignRoleSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;
using OneAccess.Application.Features.SubSystems.Commands.RevokeRoleSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.RevokeUserSubSystemAccess;
using OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;
using OneAccess.Application.Features.SubSystems.Queries.GetMySubSystems;
using OneAccess.Application.Features.SubSystems.Queries.GetRoleSubSystems;
using OneAccess.Application.Features.SubSystems.Queries.GetSubSystems;
using OneAccess.Application.Features.SubSystems.Queries.GetUserSubSystems;

namespace OneAccess.API.Endpoints;

/// <summary>
/// SubSystems and sub-system access governance endpoints per OneAccess.md Section 14.
/// </summary>
public static class SubSystemEndpoints
{
    public static IEndpointRouteBuilder MapSubSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var subSystemsGroup = app.MapGroup("/api/subsystems")
            .WithTags("SubSystems");

        // GET /api/subsystems - List all registered sub-systems (admin view)
        subSystemsGroup.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSubSystemsQuery(), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("subsystem.view")
        .WithName("GetSubSystems");

        // GET /api/subsystems/mine - Effective accessible sub-systems for the current user
        subSystemsGroup.MapGet("/mine", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMySubSystemsQuery(), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("GetMySubSystems");

        // POST /api/subsystems - Register a new sub-system
        subSystemsGroup.MapPost("/", async ([FromBody] RegisterSubSystemCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded)
            {
                return result.ToHttpResult();
            }
            return Results.Created($"/api/subsystems/{result.Value!.Id}", result.Value);
        })
        .RequirePermission("subsystem.register")
        .WithName("RegisterSubSystem");

        // PUT /api/subsystems/{id} - Update a sub-system
        subSystemsGroup.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateSubSystemRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateSubSystemCommand(id, request.Name, request.BaseUrl, request.Audience, request.IsActive);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("subsystem.update")
        .WithName("UpdateSubSystem");

        // Role-SubSystem mappings
        var rolesGroup = app.MapGroup("/api/roles")
            .WithTags("Roles");

        // GET /api/roles/{id}/subsystems - List role's sub-system restrictions
        rolesGroup.MapGet("/{id:guid}/subsystems", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRoleSubSystemsQuery(id), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("subsystem.view")
        .WithName("GetRoleSubSystems");

        // POST /api/roles/{id}/subsystems - Add sub-system to role allow-list
        rolesGroup.MapPost("/{id:guid}/subsystems", async (Guid id, [FromBody] AssignRoleSubSystemRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AssignRoleSubSystemAccessCommand(id, request.SubSystemId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("rolesubsystem.assign")
        .WithName("AssignRoleSubSystemAccess");

        // DELETE /api/roles/{id}/subsystems/{subSystemId} - Remove sub-system from role allow-list
        rolesGroup.MapDelete("/{id:guid}/subsystems/{subSystemId:guid}", async (Guid id, Guid subSystemId, ISender sender, CancellationToken ct) =>
        {
            var command = new RevokeRoleSubSystemAccessCommand(id, subSystemId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("rolesubsystem.revoke")
        .WithName("RevokeRoleSubSystemAccess");

        // User-SubSystem mappings
        var usersGroup = app.MapGroup("/api/users")
            .WithTags("Users");

        // GET /api/users/{id}/subsystems - List user's sub-system overrides
        usersGroup.MapGet("/{id:guid}/subsystems", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserSubSystemsQuery(id), ct);
            return result.ToHttpResult();
        })
        .RequirePermission("user.view")
        .WithName("GetUserSubSystems");

        // POST /api/users/{id}/subsystems - Add sub-system to user override allow-list
        usersGroup.MapPost("/{id:guid}/subsystems", async (Guid id, [FromBody] AssignUserSubSystemRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AssignUserSubSystemAccessCommand(id, request.SubSystemId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("usersubsystem.assign")
        .WithName("AssignUserSubSystemAccess");

        // DELETE /api/users/{id}/subsystems/{subSystemId} - Remove sub-system from user override allow-list
        usersGroup.MapDelete("/{id:guid}/subsystems/{subSystemId:guid}", async (Guid id, Guid subSystemId, ISender sender, CancellationToken ct) =>
        {
            var command = new RevokeUserSubSystemAccessCommand(id, subSystemId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequirePermission("usersubsystem.revoke")
        .WithName("RevokeUserSubSystemAccess");

        return app;
    }
}

public record UpdateSubSystemRequest(string Name, string BaseUrl, string Audience, bool IsActive);

public record AssignRoleSubSystemRequest(Guid SubSystemId);

public record AssignUserSubSystemRequest(Guid SubSystemId);
