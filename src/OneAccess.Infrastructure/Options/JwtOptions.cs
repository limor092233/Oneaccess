using System.ComponentModel.DataAnnotations;

namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration options for JWT token generation and validation.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "OneAccess";

    [Required]
    public string Audience { get; set; } = "OneAccess.Client";

    public string ActiveKeyId { get; set; } = string.Empty;

    public string SigningKeysDirectory { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; set; } = 7;

    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 0;
}
