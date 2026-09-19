using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Domain.Entities;
using OneAccess.Infrastructure.Logging;
using OneAccess.Infrastructure.Options;
using OneAccess.Infrastructure.Persistence;
using OneAccess.Infrastructure.Persistence.Repositories;
using OneAccess.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using System.IO;
using Xunit;

namespace OneAccess.Application.UnitTests.Features.Setup;

public class SerilogConfiguratorAndSetupCodeLoggingTests
{
    [Fact]
    public async Task SetupCodeService_EmitsSetupCodeWithIsSetupCodeProperty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OneAccessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new OneAccessDbContext(options);
        var uow = new UnitOfWork(db);
        var setupOptions = Microsoft.Extensions.Options.Options.Create(new SetupOptions
        {
            CodeLength = 8,
            CodeExpiryMinutes = 15,
            ConsoleOutputEnabled = true
        });

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        // Serilog in-memory sink to capture events
        var events = new List<LogEvent>();
        var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new DelegatingSink(e => events.Add(e)))
            .CreateLogger();

        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog(serilogLogger);
        var logger = loggerFactory.CreateLogger<SetupCodeService>();

        var service = new SetupCodeService(db, uow, setupOptions, dateTimeProvider, logger);

        // Act
        var code = await service.GenerateAndStoreCodeAsync();

        // Assert
        events.Should().ContainSingle();
        var logEvent = events.First();
        logEvent.Properties.Should().ContainKey(SerilogConfigurator.SetupCodePropertyName);
        var isSetupCode = logEvent.Properties[SerilogConfigurator.SetupCodePropertyName] as ScalarValue;
        isSetupCode.Should().NotBeNull();
        isSetupCode!.Value.Should().Be(true);
        logEvent.RenderMessage().Should().Contain(code);
    }

    [Fact]
    public async Task SmokeTest_UsingRealAppSettings_SetupCodeLoggedToConsole_ExcludedFromDiskLogFile()
    {
        // Arrange - Load REAL appsettings.json from src/OneAccess.API
        var appsettingsPath = GetAppsettingsPath();
        File.Exists(appsettingsPath).Should().BeTrue("real src/OneAccess.API/appsettings.json must exist");

        var testLogDir = Path.Combine(Path.GetTempPath(), $"oneaccess_smoke_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testLogDir);
        var testLogFilePattern = Path.Combine(testLogDir, "oneaccess-.log");

        try
        {
            // Build configuration using real appsettings.json, overriding the log path to a clean temp directory
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(appsettingsPath, optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Serilog:File:Path"] = testLogFilePattern,
                    ["Setup:ConsoleOutputEnabled"] = "true" // Enable console output for first-run
                })
                .Build();

            var consoleLogs = new List<LogEvent>();

            // Configure Serilog using SerilogConfigurator with the REAL app configuration
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Sink(new DelegatingSink(e => consoleLogs.Add(e)));

            SerilogConfigurator.Configure(loggerConfig, configuration);
            var serilogLogger = loggerConfig.CreateLogger();

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(serilogLogger);
            var logger = loggerFactory.CreateLogger<SetupCodeService>();

            var options = new DbContextOptionsBuilder<OneAccessDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new OneAccessDbContext(options);
            var uow = new UnitOfWork(db);
            var setupOptions = Microsoft.Extensions.Options.Options.Create(new SetupOptions
            {
                CodeLength = 8,
                CodeExpiryMinutes = 15,
                ConsoleOutputEnabled = true
            });

            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

            var service = new SetupCodeService(db, uow, setupOptions, dateTimeProvider, logger);

            // Act 1: Standard application log
            logger.LogInformation("OneAccess backend startup initialized from real appsettings.json.");

            // Act 2: Emit first-run setup code
            var setupCode = await service.GenerateAndStoreCodeAsync();

            // Flush & close Serilog file sink
            serilogLogger.Dispose();

            // Assert 1: Console sink received both the startup log and the raw setup code
            consoleLogs.Should().HaveCount(2);
            consoleLogs.Any(e => e.RenderMessage().Contains(setupCode)).Should().BeTrue();
            consoleLogs.Any(e => e.RenderMessage().Contains("[SETUP] First-Run System Administrator Setup")).Should().BeTrue();

            // Assert 2: File sink on disk was generated via SerilogConfigurator and real config
            var logFiles = Directory.GetFiles(testLogDir, "*.log");
            logFiles.Should().NotBeEmpty("SerilogConfigurator should have created the log file on disk");

            var fileContent = await File.ReadAllTextAsync(logFiles.First());
            fileContent.Should().Contain("OneAccess backend startup initialized from real appsettings.json.");
            fileContent.Should().NotContain(setupCode, "Setup code must NEVER appear in disk logs (OneAccess.md Section 12)");
            fileContent.Should().NotContain("[SETUP] First-Run System Administrator Setup");
            fileContent.Should().NotContain(SerilogConfigurator.SetupCodePropertyName);
        }
        finally
        {
            if (Directory.Exists(testLogDir))
            {
                try { Directory.Delete(testLogDir, true); } catch { }
            }
        }
    }

    private static string GetAppsettingsPath()
    {
        var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "OneAccess.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException("Could not locate solution root containing OneAccess.sln");
        }

        return Path.Combine(currentDir.FullName, "src", "OneAccess.API", "appsettings.json");
    }

    private class DelegatingSink : Serilog.Core.ILogEventSink
    {
        private readonly Action<LogEvent> _emit;
        public DelegatingSink(Action<LogEvent> emit) => _emit = emit;
        public void Emit(LogEvent logEvent) => _emit(logEvent);
    }
}
