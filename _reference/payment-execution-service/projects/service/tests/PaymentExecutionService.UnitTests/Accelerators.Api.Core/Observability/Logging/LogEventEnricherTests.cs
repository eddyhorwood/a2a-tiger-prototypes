// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using FluentAssertions;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xero.Accelerators.Api.Core.Observability.Logging;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Observability.Logging;

public class LogEventEnricherTests
{
    [Fact]
    public void GitHashEnricher_WhenCalled_AddsGitHashPropertyToLogEvent()
    {
        // Arrange
        const string GitHash = "unknown"; // ComponentVersion.FromAssembly().GitHash returns "unknown"
        var logEvent = new LogEvent(DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("GitHashEnricher", Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());
        var mockPropertyFactory = new Mock<ILogEventPropertyFactory>();
        mockPropertyFactory
            .Setup(factory => factory.CreateProperty("GitHash", It.IsAny<string>(), false))
            .Returns(new LogEventProperty("GitHash", new ScalarValue(GitHash)));
        var enricher = new GitHashEnricher();

        // Act
        enricher.Enrich(logEvent, mockPropertyFactory.Object);

        // Assert
        logEvent.Properties.Should().HaveCount(1)
            .And.ContainKey("GitHash")
            .And.ContainValue(new ScalarValue(GitHash));
    }

    [Fact]
    public void LogLevelEnricher_WhenCalled_AddsLogLevelPropertyToLogEvent()
    {
        // Arrange
        var logEvent = new LogEvent(DateTimeOffset.Now,
            LogEventLevel.Warning,
            null,
            new MessageTemplate("LogLevelEnricher", Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());
        var mockPropertyFactory = new Mock<ILogEventPropertyFactory>();
        mockPropertyFactory
            .Setup(factory => factory.CreateProperty("Level", It.IsAny<string>(), false))
            .Returns(new LogEventProperty("Level", new ScalarValue("Warning")));
        var enricher = new LogLevelEnricher();

        // Act
        enricher.Enrich(logEvent, mockPropertyFactory.Object);

        // Assert
        logEvent.Properties.Should().HaveCount(1)
            .And.ContainKey("Level")
            .And.ContainValue(new ScalarValue("Warning"));
    }
}
