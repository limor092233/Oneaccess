namespace OneAccess.Infrastructure.Options;

/// <summary>
/// CORS configuration options for local development.
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE" };
    public bool AllowCredentials { get; set; } = true;
}
