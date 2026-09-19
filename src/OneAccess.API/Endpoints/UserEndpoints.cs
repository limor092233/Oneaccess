using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.API.Authorization;
using OneAccess.Application.Common.Models;
using OneAccess.Application.Features.Users.Commands.AssignRole;
using OneAccess.Application.Features.Users.Commands.CreateUser;
using OneAccess.Application.Features.Users.Commands.DeleteUser;
using OneAccess.Application.Features.Users.Commands.UpdateUser;
using OneAccess.Application.Features.Users.Queries.GetUserById;
using OneAccess.Application.Features.Users.Queries.GetUsers;

namespace OneAccess.API.Endpoints;

/// <summary>
/// User management endpoints per OneAccess.md Section 14.
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users");

        // GET /api/users - List users
        group.MapGet("/", async (
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetUsersQuery(pageNumber, pageSize, search), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("user.view")
        .WithName("GetUsers");

        // GET /api/users/{id} - Get user by ID
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserByIdQuery(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("user.view")
        .WithName("GetUserById");

        // POST /api/users - Create user
        group.MapPost("/", async ([FromBody] CreateUserCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            if (!result.Succeeded)
            {
                return ToHttpResult(result);
            }
            return Results.Created($"/api/users/{result.Value!.Id}", result.Value);
        })
        .RequirePermission("user.create")
        .WithName("CreateUser");

        // PUT /api/users/{id} - Update user
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateUserRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateUserCommand(id, request.Email, request.FullName, request.DivisionId, request.SectionId, request.Status);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("user.update")
        .WithName("UpdateUser");

        // DELETE /api/users/{id} - Soft delete user
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteUserCommand(id), ct);
            return ToHttpResult(result);
        })
        .RequirePermission("user.delete")
        .WithName("DeleteUser");

        // POST /api/users/{id}/roles - Assign role
        group.MapPost("/{id:guid}/roles", async (Guid id, [FromBody] AssignRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AssignRoleCommand(id, request.RoleId);
            var result = await sender.Send(command, ct);
            return ToHttpResult(result);
        })
        .RequirePermission("user.update")
        .WithName("AssignRole");

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

public record UpdateUserRequest(
    string Email,
    string FullName,
    Guid? DivisionId = null,
    Guid? SectionId = null,
    OneAccess.Domain.Enums.UserStatus? Status = null
);

public record AssignRoleRequest(Guid RoleId);
