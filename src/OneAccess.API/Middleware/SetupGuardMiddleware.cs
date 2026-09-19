using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.API.Middleware;

/// <summary>
/// Blocks all /api/setup endpoints with 403 Forbidden once a System Administrator exists (OneAccess.md Section 6 & 11).
/// </summary>
public class SetupGuardMiddleware
{
    private readonly RequestDelegate _next;

    public SetupGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISetupCodeService setupCodeService)
    {
        var path = context.Request.Path.Value;

        // Only guard /api/setup routes (except GET /api/setup/status which needs to report status or can also be checked)
        // Wait: OneAccess.md Section 6:
        // "Once a System Administrator exists -> setup is CLOSED. GET /api/setup/status returns { isSetupRequired: false }, POST /api/setup/initialize returns 403 Forbidden"
        // And Section 11: SetupGuardMiddleware blocks setup routes once a SysAdmin exists
        if (path != null && path.StartsWith("/api/setup/initialize", StringComparison.OrdinalIgnoreCase))
        {
            var isSetupRequired = await setupCodeService.IsSetupRequiredAsync(context.RequestAborted);
            if (!isSetupRequired)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = "Forbidden",
                    Detail = "System Administrator setup has already been completed. This endpoint is permanently disabled.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                return;
            }
        }

        await _next(context);
    }
}
