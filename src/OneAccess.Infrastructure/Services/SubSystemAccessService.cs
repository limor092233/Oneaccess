using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;

namespace OneAccess.Infrastructure.Services;

/// <summary>
/// Service resolving effective sub-system visibility for users.
/// </summary>
public class SubSystemAccessService : ISubSystemAccessService
{
    private readonly IReadDbContext _readDbContext;
    private readonly ICacheService _cacheService;

    public SubSystemAccessService(IReadDbContext readDbContext, ICacheService cacheService)
    {
        _readDbContext = readDbContext;
        _cacheService = cacheService;
    }

    public async Task<bool> HasAccessAsync(Guid userId, Guid subSystemId, CancellationToken ct = default)
    {
        var accessible = await GetAccessibleSubSystemsAsync(userId, ct);
        return accessible.Any(s => s.Id == subSystemId && s.IsActive);
    }

    public async Task<IReadOnlyList<SubSystem>> GetAccessibleSubSystemsAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"subsystems:{userId}";
        var cached = await _cacheService.GetAsync<List<SubSystemSummary>>(cacheKey, ct);
        if (cached != null)
        {
            return cached.Select(c => new SubSystem
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                BaseUrl = c.BaseUrl,
                Audience = c.Audience,
                IsActive = c.IsActive
            }).ToList();
        }

        var user = await _readDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || user.Status != UserStatus.Active)
        {
            return Array.Empty<SubSystem>();
        }

        List<SubSystem> result;

        // 1. System Administrator sees all active sub-systems
        if (user.IsSystemAdministrator)
        {
            result = await _readDbContext.SubSystems
                .Where(s => s.IsActive)
                .ToListAsync(ct);
        }
        else
        {
            // 2. Check per-user override
            var hasUserOverrides = await _readDbContext.UserSubSystemAccesses
                .AnyAsync(usa => usa.UserId == userId, ct);

            if (hasUserOverrides)
            {
                result = await (from usa in _readDbContext.UserSubSystemAccesses
                                join s in _readDbContext.SubSystems on usa.SubSystemId equals s.Id
                                where usa.UserId == userId && s.IsActive
                                select s)
                                .ToListAsync(ct);
            }
            else
            {
                // 3. Check role-level restrictions
                var userRoleIds = await _readDbContext.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync(ct);

                var hasRoleRestrictions = await _readDbContext.RoleSubSystemAccesses
                    .AnyAsync(rsa => userRoleIds.Contains(rsa.RoleId), ct);

                if (hasRoleRestrictions)
                {
                    result = await (from rsa in _readDbContext.RoleSubSystemAccesses
                                    join s in _readDbContext.SubSystems on rsa.SubSystemId equals s.Id
                                    where userRoleIds.Contains(rsa.RoleId) && s.IsActive
                                    select s)
                                    .Distinct()
                                    .ToListAsync(ct);
                }
                else
                {
                    // 4. Default: all active sub-systems
                    result = await _readDbContext.SubSystems
                        .Where(s => s.IsActive)
                        .ToListAsync(ct);
                }
            }
        }

        var summaries = result.Select(s => new SubSystemSummary(s.Id, s.Code, s.Name, s.BaseUrl, s.Audience, s.IsActive)).ToList();
        await _cacheService.SetAsync(cacheKey, summaries, TimeSpan.FromMinutes(5), ct);

        return result;
    }

    private record SubSystemSummary(Guid Id, string Code, string Name, string BaseUrl, string Audience, bool IsActive);
}
