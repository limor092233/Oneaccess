using System.ComponentModel.DataAnnotations;

namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Options for the session cookie.
/// </summary>
public class CookieOptions
{
    public const string SectionName = "Cookie";

    [Required]
    public string Name { get; set; } = "oneaccess.session";

    public bool HttpOnly { get; set; } = true;

    public bool Secure { get; set; } = true;

    [Required]
    public string SameSite { get; set; } = "Strict";

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 480;
}
