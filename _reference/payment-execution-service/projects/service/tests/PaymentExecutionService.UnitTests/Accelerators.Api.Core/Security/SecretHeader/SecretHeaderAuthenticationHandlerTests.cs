// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xero.Accelerators.Api.Core.Security.SecretHeader;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.SecretHeader;

public class SecretHeaderTests
{
    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenMissingHeaderValue_ThenFailsWithCorrectMessage()
    {
        // Arrange
        var options = new SecretHeaderOptions();
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should().Be("Secret header name missing from config.");
    }

    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenMissingSecretKeys_ThenAuthenticationFails()
    {
        // Arrange
        var secretHeaderName = "X-Test-Secret";
        var options = new SecretHeaderOptions { SecretHeaderName = secretHeaderName };
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();

        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure?.Message.Should().Be("Secret header keys missing from config.");
    }

    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenMissingHeader_ThenAuthenticationFails()
    {
        // Arrange
        var secretHeaderName = "X-Test-Secret";
        var secretKeys = "SECRET";
        var options = new SecretHeaderOptions { SecretHeaderName = secretHeaderName, SecretKeys = secretKeys };
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();

        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure?.Message.Should().Be("Secret header missing.");
    }

    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenMissingHeaderValue_ThenAuthenticationFails()
    {
        // Arrange
        var secretHeaderName = "X-Test-Secret";
        var secretKeys = "SECRET";
        var options = new SecretHeaderOptions { SecretHeaderName = secretHeaderName, SecretKeys = secretKeys };
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers[secretHeaderName] = "";
        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure?.Message.Should().Be("Secret header value missing.");
    }

    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenInvalidHeaderValue_ThenAuthenticationFails()
    {
        // Arrange
        var secretHeaderName = "X-Test-Secret";
        var secretKeys = "SECRET";
        var options = new SecretHeaderOptions { SecretHeaderName = secretHeaderName, SecretKeys = secretKeys };
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers[secretHeaderName] = "NOT-SECRET";
        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure?.Message.Should().Be("Secret header value invalid.");
    }

    [Fact]
    public async Task GivenSecretHeaderAuthenticationHandler_WhenValidHeaderValue_ThenAuthenticationSucceeds()
    {
        // Arrange
        var secretHeaderName = "X-Test-Secret";
        var secretKeys = "PASSWORD, SECRET";
        var options = new SecretHeaderOptions { SecretHeaderName = secretHeaderName, SecretKeys = secretKeys };
        var optionsMonitor = new Mock<IOptionsMonitor<SecretHeaderOptions>>(MockBehavior.Strict);
        optionsMonitor.Setup(x => x.Get("TestSecretHeaderAuthenticationScheme")).Returns(options);
        var logger = new Mock<ILogger<SecretHeaderAuthenticationHandler>>();
        var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        loggerFactory.Setup(x => x.CreateLogger(typeof(SecretHeaderAuthenticationHandler).FullName!)).Returns(logger.Object);

        var encoder = new Mock<UrlEncoder>(MockBehavior.Strict);
        var handler = new SecretHeaderAuthenticationHandler(optionsMonitor.Object, loggerFactory.Object, encoder.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers[secretHeaderName] = "SECRET";
        await handler.InitializeAsync(new AuthenticationScheme("TestSecretHeaderAuthenticationScheme", "Secret Header Authentication", typeof(SecretHeaderAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
