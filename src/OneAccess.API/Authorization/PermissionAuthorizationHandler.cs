using Microsoft.AspNetCore.Authorization;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.API.Authorization;

/// <summary>
/// Live, per-request authorization handler that validates permissions via ICurrentUserService.GetPermissionsAsync()
/// (backed by short-lived Redis cache and invalidated immediately on role/permission mutations).
/// Never checks static claims baked into the initial login ticket.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public PermissionAuthorizationHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return;
        }

        // System Administrator unconditionally holds all permissions (evaluated live per-request)
        if (await _currentUserService.IsSystemAdministratorAsync())
        {
            context.Succeed(requirement);
            return;
        }

        // Fetch live permissions (cached in Redis, invalidated on mutation per OneAccess.md Section 7 & 10)
        var permissions = await _currentUserService.GetPermissionsAsync();

        if (permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
    }
}
