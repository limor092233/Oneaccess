namespace OneAccess.Infrastructure.Options;

/// <summary>
/// Options governing first-run System Administrator setup.
/// </summary>
public class SetupOptions
{
    public const string SectionName = "Setup";

    public int CodeLength { get; set; } = 8;
    public int CodeExpiryMinutes { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;
    public bool ConsoleOutputEnabled { get; set; } = true;
}
