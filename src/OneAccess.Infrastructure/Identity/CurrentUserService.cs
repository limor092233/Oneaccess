using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Enums;

namespace OneAccess.Infrastructure.Identity;

/// <summary>
/// Provides identity and authorization scope information for the current HTTP request.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadDbContext _readDbContext;
    private readonly ICacheService _cacheService;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IReadDbContext readDbContext,
        ICacheService cacheService)
    {
        _httpContextAccessor = httpContextAccessor;
        _readDbContext = readDbContext;
        _cacheService = cacheService;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.FindFirst("name")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User == null) return Array.Empty<string>();
            return User.FindAll(ClaimTypes.Role)
                .Concat(User.FindAll("role"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }
    }

    public bool IsSystemAdministrator => Roles.Contains(SystemRoles.SystemAdministrator);

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken ct = default)
    {
        if (!UserId.HasValue) return Array.Empty<string>();

        var cacheKey = $"permissions:{UserId.Value}";
        var cached = await _cacheService.GetAsync<List<string>>(cacheKey, ct);
        if (cached != null) return cached;

        // If System Administrator, return all permissions
        if (IsSystemAdministrator)
        {
            var allPerms = await _readDbContext.Permissions
                .Select(p => p.Code)
                .ToListAsync(ct);

            await _cacheService.SetAsync(cacheKey, allPerms, TimeSpan.FromMinutes(5), ct);
            return allPerms;
        }

        // Resolve user permissions via RolePermissions
        var userPermissions = await (from ur in _readDbContext.UserRoles
                                    join rp in _readDbContext.RolePermissions on ur.RoleId equals rp.RoleId
                                    join p in _readDbContext.Permissions on rp.PermissionId equals p.Id
                                    where ur.UserId == UserId.Value
                                    select p.Code)
                                    .Distinct()
                                    .ToListAsync(ct);

        await _cacheService.SetAsync(cacheKey, userPermissions, TimeSpan.FromMinutes(5), ct);
        return userPermissions;
    }

    public async Task<IReadOnlyList<Guid>> GetAssignedDivisionIdsAsync(CancellationToken ct = default)
    {
        if (!UserId.HasValue) return Array.Empty<Guid>();

        var cacheKey = $"assigned_divisions:{UserId.Value}";
        var cached = await _cacheService.GetAsync<List<Guid>>(cacheKey, ct);
        if (cached != null) return cached;

        var divisionIds = await _readDbContext.UserDivisionAssignments
            .Where(uda => uda.UserId == UserId.Value)
            .Select(uda => uda.DivisionId)
            .ToListAsync(ct);

        await _cacheService.SetAsync(cacheKey, divisionIds, TimeSpan.FromMinutes(5), ct);
        return divisionIds;
    }
}
