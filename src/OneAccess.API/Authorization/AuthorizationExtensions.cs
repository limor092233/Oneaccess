using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace OneAccess.API.Authorization;

/// <summary>
/// Route endpoint extension methods for declarative permission authorization.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Enforces that the endpoint requires the specified permission code evaluated per-request by PermissionAuthorizationHandler.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(permission));
        });
    }

    /// <summary>
    /// Enforces that the route group requires the specified permission code evaluated per-request by PermissionAuthorizationHandler.
    /// </summary>
    public static RouteGroupBuilder RequirePermission(this RouteGroupBuilder builder, string permission)
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(permission));
        });
    }
}
