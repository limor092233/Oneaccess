using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public async Task SmokeTest_SetupCodeLoggedToConsole_ExcludedFromDiskLogFile()
    {
        // Arrange
        var tempLogPath = Path.Combine(Path.GetTempPath(), $"oneaccess_test_{Guid.NewGuid():N}.log");
        var consoleLogs = new List<LogEvent>();

        try
        {
            var serilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                // Console sink: unfiltered (receives setup code)
                .WriteTo.Sink(new DelegatingSink(e => consoleLogs.Add(e)))
                // Non-console / File sink on disk: filtered via SerilogConfigurator
                .WriteToNonConsole(sink => sink.WriteTo.File(tempLogPath))
                .CreateLogger();

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

            // Emit a normal startup log
            logger.LogInformation("OneAccess backend startup initialized.");

            // Act: Generate setup code (which emits setup code to logger with IsSetupCode)
            var setupCode = await service.GenerateAndStoreCodeAsync();

            // Flush Serilog logger to disk
            serilogLogger.Dispose();

            // Assert 1: Console sink contains both the normal log and the setup code
            consoleLogs.Should().HaveCount(2);
            consoleLogs.Any(e => e.RenderMessage().Contains(setupCode)).Should().BeTrue();
            consoleLogs.Any(e => e.RenderMessage().Contains("[SETUP] First-Run System Administrator Setup")).Should().BeTrue();

            // Assert 2: File on disk contains the normal startup log, but NEVER contains the setup code
            File.Exists(tempLogPath).Should().BeTrue();
            var fileText = await File.ReadAllTextAsync(tempLogPath);
            fileText.Should().Contain("OneAccess backend startup initialized.");
            fileText.Should().NotContain(setupCode);
            fileText.Should().NotContain("[SETUP] First-Run System Administrator Setup");
            fileText.Should().NotContain(SerilogConfigurator.SetupCodePropertyName);
        }
        finally
        {
            if (File.Exists(tempLogPath))
            {
                try { File.Delete(tempLogPath); } catch { }
            }
        }
    }

    private class DelegatingSink : Serilog.Core.ILogEventSink
    {
        private readonly Action<LogEvent> _emit;
        public DelegatingSink(Action<LogEvent> emit) => _emit = emit;
        public void Emit(LogEvent logEvent) => _emit(logEvent);
    }
}
