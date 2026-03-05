using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecutionService.Middleware;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware;

public class ExceptionLoggingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionLoggingMiddleware>> _mockLogger;
    private readonly RequestDelegate _nextMiddleware;

    public ExceptionLoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ExceptionLoggingMiddleware>>();
        _nextMiddleware = new RequestDelegate(context => Task.CompletedTask);
    }

    [Fact]
    public async Task GivenNoException_WhenMiddlewareIsInvoked_ThenShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new ExceptionLoggingMiddleware(_nextMiddleware, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await middleware.Invoke(context);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenUnhandledException_WhenMiddlewareIsInvoked_ThenShouldLogErrorAndRethrow()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var testException = new InvalidOperationException("Test exception");
        var failingMiddleware = new ExceptionLoggingMiddleware(_ => throw testException, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await failingMiddleware.Invoke(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "Payment execution service unhandled exception." && @type.Name == "FormattedLogValues"),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
