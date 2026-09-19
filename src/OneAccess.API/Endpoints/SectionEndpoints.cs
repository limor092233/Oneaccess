using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.Sections.Commands.CreateSection;
using OneAccess.Application.Features.Sections.Commands.DeleteSection;
using OneAccess.Application.Features.Sections.Commands.UpdateSection;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Section management endpoints per OneAccess.md Section 14.
/// </summary>
public static class SectionEndpoints
{
    public static IEndpointRouteBuilder MapSectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sections")
            .WithTags("Sections");

        // POST /api/sections - Create section
        group.MapPost("/", async ([FromBody] CreateSectionCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded)
            {
                return ToHttpResult(result);
            }
            return Results.Created($"/api/sections/{result.Value!.Id}", result.Value);
        })
        .RequirePermission("section.create")
        .WithName("CreateSection");

        // PUT /api/sections/{id} - Update section
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateSectionRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateSectionCommand(id, request.Name, request.Description);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("section.update")
        .WithName("UpdateSection");

        // DELETE /api/sections/{id} - Delete section
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteSectionCommand(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("section.delete")
        .WithName("DeleteSection");

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

public record UpdateSectionRequest(string Name, string Description);
