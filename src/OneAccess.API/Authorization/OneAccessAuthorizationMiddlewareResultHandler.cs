using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using OneAccess.API.Common;

namespace OneAccess.API.Authorization;

/// <summary>
/// Custom authorization middleware result handler ensuring all 401 Unauthorized and 403 Forbidden
/// authorization policy failures return consistent RFC 7807 ProblemDetails responses via ResultExtensions.ToProblem.
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
            var problemResult = ResultExtensions.ToProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication is required to access this resource.",
                "Unauthorized",
                context.Request.Path
            );
            await problemResult.ExecuteAsync(context);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var problemResult = ResultExtensions.ToProblem(
                StatusCodes.Status403Forbidden,
                "Access denied: you do not have permission to access this resource.",
                "Forbidden",
                context.Request.Path
            );
            await problemResult.ExecuteAsync(context);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
