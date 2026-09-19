using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace OneAccess.Infrastructure.Logging;

/// <summary>
/// Configures Serilog from IConfiguration and custom filters.
/// Ensures that setup code logs are excluded from non-console sinks (OneAccess.md Section 12).
/// </summary>
public static class SerilogConfigurator
{
    public const string SetupCodePropertyName = "IsSetupCode";

    /// <summary>
    /// Predicate that matches log events containing the IsSetupCode property.
    /// Used by non-console sinks to exclude setup code events from being logged to external/shared log sinks.
    /// </summary>
    public static readonly Func<LogEvent, bool> IsSetupCodeEvent =
        le => le.Properties.ContainsKey(SetupCodePropertyName);

    public static void Configure(LoggerConfiguration loggerConfig, IConfiguration configuration)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();
    }
}

