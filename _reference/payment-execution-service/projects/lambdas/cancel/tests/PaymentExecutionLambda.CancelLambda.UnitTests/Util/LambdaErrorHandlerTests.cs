using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecutionLambda.CancelLambda.Util;

namespace PaymentExecutionLambda.CancelLambdaUnitTests.Util;

public class LambdaErrorHandlerTests
{
    [Fact]
    public void GivenSuccessResult_WhenClassifyError_ThenReturnsSuccessDecision()
    {
        // Arrange
        var result = Result.Ok();

        // Act
        var decision = LambdaErrorHandler.ClassifyError(result);

        // Assert
        decision.Should().Be(ErrorHandlingDecision.Success);
    }

    [Fact]
    public void GivenDependencyTransientError_WhenClassifyError_ThenReturnsRetryDecision()
    {
        // Arrange
        var error = new PaymentExecutionError("Transient error", ErrorType.DependencyTransientError, "error_code");
        var result = Result.Fail(error);

        // Act
        var decision = LambdaErrorHandler.ClassifyError(result);

        // Assert
        decision.Should().Be(ErrorHandlingDecision.RetryMessage);
    }

    [Theory]
    [InlineData(ErrorType.ClientError)]
    [InlineData(ErrorType.BadPaymentRequest)]
    [InlineData(ErrorType.PaymentFailed)]
    [InlineData(ErrorType.PaymentTransactionNotFound)]
    [InlineData(ErrorType.PaymentTransactionNotCancellable)]
    [InlineData(ErrorType.FailedDependency)]
    [InlineData(ErrorType.ValidationError)]
    public void GivenNonRetryableErrorType_WhenClassifyError_ThenReturnsDeleteDecision(ErrorType errorType)
    {
        // Arrange
        var error = new PaymentExecutionError("Non-retryable error", errorType, "error_code");
        var result = Result.Fail(error);

        // Act
        var decision = LambdaErrorHandler.ClassifyError(result);

        // Assert
        decision.Should().Be(ErrorHandlingDecision.DeleteNonRetryableMessage);
    }

    [Fact]
    public void GivenNonPaymentExecutionError_WhenClassifyError_ThenReturnsDeleteDecision()
    {
        // Arrange
        var result = Result.Fail("Unexpected error");

        // Act
        var decision = LambdaErrorHandler.ClassifyError(result);

        // Assert
        decision.Should().Be(ErrorHandlingDecision.DeleteNonRetryableMessage);
    }

    [Fact]
    public void GivenPaymentExecutionErrorWithoutErrorType_WhenClassifyError_ThenReturnsDeleteDecision()
    {
        // Arrange
        var error = new PaymentExecutionError("Error without type", System.Net.HttpStatusCode.BadRequest);
        var result = Result.Fail(error);

        // Act
        var decision = LambdaErrorHandler.ClassifyError(result);

        // Assert
        decision.Should().Be(ErrorHandlingDecision.DeleteNonRetryableMessage);
    }

    [Fact]
    public void GivenSuccessResult_WhenLogErrorDecision_ThenLogsDebugLevel()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var result = Result.Ok();
        var decision = ErrorHandlingDecision.Success;
        var messageId = "test-message-123";

        // Act
        LambdaErrorHandler.LogErrorDecision(mockLogger.Object, result, decision, messageId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("processed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GivenDeleteDecisionWithPaymentExecutionError_WhenLogErrorDecision_ThenLogsWarningWithMetadata()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var error = new PaymentExecutionError("Bad request", ErrorType.BadPaymentRequest, "error_code_123");
        var result = Result.Fail(error);
        var decision = ErrorHandlingDecision.DeleteNonRetryableMessage;
        var messageId = "test-message-456";

        // Act
        LambdaErrorHandler.LogErrorDecision(mockLogger.Object, result, decision, messageId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("will be deleted from queue as it is not retryable") &&
                    v.ToString()!.Contains("ErrorType") &&
                    v.ToString()!.Contains("ErrorCode") &&
                    v.ToString()!.Contains("HttpStatusCode")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GivenDeleteDecisionWithNonPaymentExecutionError_WhenLogErrorDecision_ThenLogsWarningWithErrorMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var result = Result.Fail("Unexpected error");
        var decision = ErrorHandlingDecision.DeleteNonRetryableMessage;
        var messageId = "test-message-789";

        // Act
        LambdaErrorHandler.LogErrorDecision(mockLogger.Object, result, decision, messageId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("will be deleted from queue as it is not retryable") &&
                    v.ToString()!.Contains("Unexpected error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GivenRetryDecisionWithPaymentExecutionError_WhenLogErrorDecision_ThenLogsErrorWithMetadata()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var error = new PaymentExecutionError("Transient error", ErrorType.DependencyTransientError, "transient_error_code");
        var result = Result.Fail(error);
        var decision = ErrorHandlingDecision.RetryMessage;
        var messageId = "test-message-101";

        // Act
        LambdaErrorHandler.LogErrorDecision(mockLogger.Object, result, decision, messageId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("will be retried and sent to DLQ if max retries exceeded") &&
                    v.ToString()!.Contains("ErrorType") &&
                    v.ToString()!.Contains("ErrorCode") &&
                    v.ToString()!.Contains("HttpStatusCode")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GivenRetryDecisionWithNonPaymentExecutionError_WhenLogErrorDecision_ThenLogsErrorWithErrorMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var result = Result.Fail("Unexpected error occurred");
        var decision = ErrorHandlingDecision.RetryMessage;
        var messageId = "test-message-202";

        // Act
        LambdaErrorHandler.LogErrorDecision(mockLogger.Object, result, decision, messageId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("will be retried and sent to DLQ if max retries exceeded") &&
                    v.ToString()!.Contains("Unexpected error occurred")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
