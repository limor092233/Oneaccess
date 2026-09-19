using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;
using OneAccess.Application.Features.Divisions.Commands.CreateDivision;
using OneAccess.Application.Features.Divisions.Commands.DeleteDivision;
using OneAccess.Application.Features.Divisions.Commands.RevokeUserDivision;
using OneAccess.Application.Features.Divisions.Commands.UpdateDivision;
using OneAccess.Application.Features.Divisions.Queries.GetDivisionAdministrators;
using OneAccess.Application.Features.Divisions.Queries.GetDivisions;
using OneAccess.Application.Features.Sections.Queries.GetSectionsByDivision;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Division management endpoints per OneAccess.md Section 14.
/// </summary>
public static class DivisionEndpoints
{
    public static IEndpointRouteBuilder MapDivisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/divisions")
            .WithTags("Divisions");

        // GET /api/divisions - List divisions
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDivisionsQuery(), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("division.view")
        .WithName("GetDivisions");

        // POST /api/divisions - Create division
        group.MapPost("/", async ([FromBody] CreateDivisionCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded)
            {
                return ToHttpResult(result);
            }
            return Results.Created($"/api/divisions/{result.Value!.Id}", result.Value);
        })
        .RequirePermission("division.create")
        .WithName("CreateDivision");

        // PUT /api/divisions/{id} - Update division
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateDivisionRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateDivisionCommand(id, request.Name, request.Description);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("division.update")
        .WithName("UpdateDivision");

        // DELETE /api/divisions/{id} - Delete division
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteDivisionCommand(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("division.delete")
        .WithName("DeleteDivision");

        // GET /api/divisions/{id}/users - List Administrators assigned to division
        group.MapGet("/{id:guid}/users", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDivisionAdministratorsQuery(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("division.view")
        .WithName("GetDivisionAdministrators");

        // POST /api/divisions/{id}/users - Assign Administrator to division
        group.MapPost("/{id:guid}/users", async (Guid id, [FromBody] AssignUserDivisionRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AssignUserDivisionCommand(id, request.UserId);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("userdivision.assign")
        .WithName("AssignUserDivision");

        // DELETE /api/divisions/{id}/users/{userId} - Revoke Administrator from division
        group.MapDelete("/{id:guid}/users/{userId:guid}", async (Guid id, Guid userId, ISender sender, CancellationToken ct) =>
        {
            var command = new RevokeUserDivisionCommand(id, userId);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("userdivision.revoke")
        .WithName("RevokeUserDivision");

        // GET /api/divisions/{id}/sections - List sections under division
        group.MapGet("/{id:guid}/sections", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSectionsByDivisionQuery(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("section.view")
        .WithName("GetSectionsByDivision");

        return app;
    }

    private static IResult ToHttpResult(Result result)
    {
        if (result.Succeeded)
        {
            return Results.Ok(new { message = "Success" });
        }

        return Results.Json(new { error = result.Error, errorCode = result.ErrorCode }, statusCode: result.StatusCode);
    }

    private static IResult ToHttpResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Results.Ok(result.Value);
        }

        return Results.Json(new { error = result.Error, errorCode = result.ErrorCode }, statusCode: result.StatusCode);
    }
}

public record UpdateDivisionRequest(string Name, string Description);

public record AssignUserDivisionRequest(Guid UserId);
