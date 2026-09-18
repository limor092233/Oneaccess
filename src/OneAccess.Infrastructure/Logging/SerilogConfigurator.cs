using Microsoft.Extensions.Configuration;
using Serilog;

namespace OneAccess.Infrastructure.Logging;

/// <summary>
/// Configures Serilog from IConfiguration and custom filters.
/// </summary>
public static class SerilogConfigurator
{
    public static void Configure(LoggerConfiguration loggerConfig, IConfiguration configuration)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();
    }
}
