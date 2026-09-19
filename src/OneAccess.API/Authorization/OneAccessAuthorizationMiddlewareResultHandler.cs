using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace OneAccess.API.Authorization;

/// <summary>
/// Custom authorization middleware result handler ensuring all 401 Unauthorized and 403 Forbidden
/// authorization policy failures return consistent JSON error payloads matching the application's ProblemDetails standard.
/// </summary>
public class OneAccessAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/problem+json";
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "Authentication is required to access this resource.",
                Instance = context.Request.Path,
                Extensions = { ["errorCode"] = "Unauthorized", ["traceId"] = context.TraceIdentifier }
            };
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/problem+json";
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = "Access denied: you do not have permission to access this resource.",
                Instance = context.Request.Path,
                Extensions = { ["errorCode"] = "Forbidden", ["traceId"] = context.TraceIdentifier }
            };
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
