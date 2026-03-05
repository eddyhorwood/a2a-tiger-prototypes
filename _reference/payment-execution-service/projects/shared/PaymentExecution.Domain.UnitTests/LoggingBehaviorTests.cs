using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Commands;

namespace PaymentExecution.Domain.UnitTests;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task GivenCommandImplementingISkipLogging_WhenHandled_ThenLoggingIsSkipped()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<ProcessCompleteMessagesCommand, ProcessCompleteMessagesCommandResponse>>>();
        var behavior = new LoggingBehavior<ProcessCompleteMessagesCommand, ProcessCompleteMessagesCommandResponse>(loggerMock.Object);
        var command = new ProcessCompleteMessagesCommand();
        var expectedResponse = new ProcessCompleteMessagesCommandResponse { IsProcessingError = false };
        var nextDelegateCalled = false;

        Task<ProcessCompleteMessagesCommandResponse> Next()
        {
            nextDelegateCalled = true;
            return Task.FromResult(expectedResponse);
        }

        // Act
        var result = await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextDelegateCalled.Should().BeTrue();
        command.Should().BeAssignableTo<ISkipLoggingBehavior>();
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenCommandNotImplementingISkipLogging_WhenHandled_ThenLoggingOccurs()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LoggingBehavior<SubmitStripePaymentCommand, Result<SubmitStripePaymentCommandResponse>>>>();
        var behavior = new LoggingBehavior<SubmitStripePaymentCommand, Result<SubmitStripePaymentCommandResponse>>(loggerMock.Object);
        var command = new SubmitStripePaymentCommand
        {
            PaymentRequestId = Guid.NewGuid(),
            XeroCorrelationId = "test-correlation-id",
            XeroTenantId = "test-tenant-id"
        };
        var expectedResponse = Result.Ok(new SubmitStripePaymentCommandResponse
        {
            PaymentIntentId = "pi_test",
            ClientSecret = "secret_test"
        });

        Task<Result<SubmitStripePaymentCommandResponse>> Next()
        {
            return Task.FromResult(expectedResponse);
        }

        // Act
        var result = await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        command.Should().NotBeAssignableTo<ISkipLoggingBehavior>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }
}

