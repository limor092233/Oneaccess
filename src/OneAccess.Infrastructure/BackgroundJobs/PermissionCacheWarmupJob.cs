using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Domain.Enums;
using OneAccess.Infrastructure.Options;
using OneAccess.Infrastructure.Persistence;

namespace OneAccess.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that proactively re-warms permission caches for active users.
/// </summary>
public class PermissionCacheWarmupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<PermissionCacheWarmupJob> _logger;

    public PermissionCacheWarmupJob(
        IServiceProvider serviceProvider,
        IOptions<BackgroundJobOptions> options,
        ILogger<PermissionCacheWarmupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OneAccessDbContext>();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var activeUsers = await dbContext.Users
                    .Where(u => u.Status == UserStatus.Active)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var user in activeUsers)
                {
                    var cacheKey = $"permissions:{user.Id}";
                    List<string> perms;

                    if (user.IsSystemAdministrator)
                    {
                        perms = await dbContext.Permissions.Select(p => p.Code).ToListAsync(stoppingToken);
                    }
                    else
                    {
                        perms = await (from ur in dbContext.UserRoles
                                       join rp in dbContext.RolePermissions on ur.RoleId equals rp.RoleId
                                       join p in dbContext.Permissions on rp.PermissionId equals p.Id
                                       where ur.UserId == user.Id
                                       select p.Code)
                                       .Distinct()
                                       .ToListAsync(stoppingToken);
                    }

                    await cacheService.SetAsync(cacheKey, perms, TimeSpan.FromMinutes(15), stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to warm up permission cache.");
            }

            var interval = TimeSpan.FromMinutes(_options.PermissionCacheWarmup.IntervalMinutes);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
