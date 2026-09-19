using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;
using OneAccess.Application.Features.Setup.Queries.GetSetupStatus;

namespace OneAccess.API.Endpoints;

/// <summary>
/// Endpoints for first-run setup status check and initial System Administrator creation (OneAccess.md Section 6 & 14).
/// </summary>
public static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("Setup");

        group.MapGet("/status", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSetupStatusQuery(), ct);
            return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .AllowAnonymous()
        .WithName("GetSetupStatus");

        group.MapPost("/initialize", async (
            [FromBody] InitializeSystemAdminCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .AllowAnonymous()
        .RequireRateLimiting("Setup")
        .WithName("InitializeSystemAdmin");

        return app;
    }
}
