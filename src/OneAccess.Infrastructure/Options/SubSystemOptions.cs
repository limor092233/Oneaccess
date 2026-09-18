namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Configuration options for registered sub-systems.
/// </summary>
public class SubSystemOptions
{
    public const string SectionName = "SubSystems";

    public List<SubSystemEntry> SeedList { get; set; } = new();
}

public class SubSystemEntry
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
