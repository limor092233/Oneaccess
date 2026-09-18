namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Options for background cleanup and cache warmup jobs.
/// </summary>
public class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public RefreshTokenCleanupSettings RefreshTokenCleanup { get; set; } = new();
    public PermissionCacheWarmupSettings PermissionCacheWarmup { get; set; } = new();
}

public class RefreshTokenCleanupSettings
{
    public int IntervalHours { get; set; } = 24;
    public int RetentionDays { get; set; } = 30;
}

public class PermissionCacheWarmupSettings
{
    public int IntervalMinutes { get; set; } = 10;
}
