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

    /// <summary>
    /// Configures Serilog from IConfiguration.
    /// Console sink is unfiltered (so setup code is printed to CLI).
    /// Non-console sinks (e.g. File) are wrapped in sub-loggers that exclude IsSetupCode events.
    /// </summary>
    public static void Configure(LoggerConfiguration loggerConfig, IConfiguration configuration)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();

        var logFilePath = configuration["Serilog:File:Path"] ?? configuration["Logging:FilePath"];
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            var rollingIntervalStr = configuration["Serilog:File:RollingInterval"];
            var rollingInterval = Enum.TryParse<RollingInterval>(rollingIntervalStr, true, out var parsedInterval)
                ? parsedInterval
                : RollingInterval.Day;

            var retainedFiles = configuration.GetValue<int?>("Serilog:File:RetainedFileCountLimit") ?? 30;

            loggerConfig.WriteTo.Logger(lc => lc
                .Filter.ByExcluding(IsSetupCodeEvent)
                .WriteTo.File(logFilePath, rollingInterval: rollingInterval, retainedFileCountLimit: retainedFiles));
        }
    }

    /// <summary>
    /// Extension helper to attach any non-console sink with the IsSetupCode exclusion sub-pipeline.
    /// </summary>
    public static LoggerConfiguration WriteToNonConsole(
        this LoggerConfiguration loggerConfig,
        Action<LoggerConfiguration> configureSink)
    {
        return loggerConfig.WriteTo.Logger(lc =>
        {
            lc.Filter.ByExcluding(IsSetupCodeEvent);
            configureSink(lc);
        });
    }
}


