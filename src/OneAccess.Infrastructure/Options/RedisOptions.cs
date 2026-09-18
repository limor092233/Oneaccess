namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration options for Redis cache and token revocation.
/// </summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "OneAccess:";
}
