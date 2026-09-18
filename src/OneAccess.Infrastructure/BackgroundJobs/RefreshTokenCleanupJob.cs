using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Options;
using OneAccess.Infrastructure.Persistence;

namespace OneAccess.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that periodically cleans up expired or revoked refresh tokens past retention policy.
/// </summary>
public class RefreshTokenCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(
        IServiceProvider serviceProvider,
        IOptions<BackgroundJobOptions> options,
        ILogger<RefreshTokenCleanupJob> logger)
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

                var cutoff = DateTime.UtcNow.AddDays(-_options.RefreshTokenCleanup.RetentionDays);
                var expiredTokens = await dbContext.RefreshTokens
                    .Where(t => (t.RevokedAt != null && t.RevokedAt < cutoff) || t.ExpiresAt < cutoff)
                    .ToListAsync(stoppingToken);

                if (expiredTokens.Count != 0)
                {
                    dbContext.RefreshTokens.RemoveRange(expiredTokens);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens.", expiredTokens.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred executing RefreshTokenCleanupJob.");
            }

            var interval = TimeSpan.FromHours(_options.RefreshTokenCleanup.IntervalHours);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
