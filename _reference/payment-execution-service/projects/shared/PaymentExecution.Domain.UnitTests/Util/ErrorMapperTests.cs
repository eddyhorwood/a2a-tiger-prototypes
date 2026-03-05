using System.Net;
using FluentAssertions;
using FluentResults;
using PaymentExecution.Common;
using PaymentExecution.Domain.Util;

namespace PaymentExecution.Domain.UnitTests.Util;

public class ErrorMapperTests
{
    [Fact]
    public void GivenResultWithPaymentExecutionError_WhenMapToPaymentFailedError_ThenReturnMappedError()
    {
        // Arrange
        var paymentExecutionError = new PaymentExecutionError("Test error");
        var result = Result.Fail(paymentExecutionError);

        // Act
        var mappedError = ErrorMapper.MapToPaymentFailedError(result);

        // Assert
        Assert.Equal(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed, mappedError.GetErrorCode());
        Assert.Equal(ErrorType.PaymentFailed, mappedError.GetErrorType());
        Assert.Equal("Test error", mappedError.Message);
    }

    [Fact]
    public void GivenResultWithPaymentExecutionErrorAndProviderErrorCode_WhenMapToPaymentFailedError_ThenReturnMappedError()
    {
        // Arrange
        var paymentExecutionError = new PaymentExecutionError("Test error", providerErrorCode: "provider_error_123");
        var result = Result.Fail(paymentExecutionError);

        // Act
        var mappedError = ErrorMapper.MapToPaymentFailedError(result);

        // Assert
        Assert.Equal(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed, mappedError.GetErrorCode());
        Assert.Equal(ErrorType.PaymentFailed, mappedError.GetErrorType());
        Assert.Equal("provider_error_123", mappedError.GetProviderErrorCode());
        Assert.Equal("Test error", mappedError.Message);
    }

    [Fact]
    public void GivenResultWithoutPaymentExecutionError_WhenMapToPaymentFailedError_ThenThrowNotImplementedException()
    {
        // Arrange
        var result = Result.Fail("Generic error");

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() => ErrorMapper.MapToPaymentFailedError(result));
        Assert.Equal("Error should be of uniform type", exception.Message);
    }

    [Fact]
    public void GivenResultWithPaymentExecutionError_WhenMapToBadPaymentRequestError_ThenReturnMappedError()
    {
        // Arrange
        var baseError = new PaymentExecutionError("Test error");
        var result = Result.Fail(baseError);

        // Act
        var mappedError = ErrorMapper.MapToBadPaymentRequestError(result);

        // Assert
        Assert.Equal(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed, mappedError.GetErrorCode());
        Assert.Equal(ErrorType.BadPaymentRequest, mappedError.GetErrorType());
        Assert.Null(mappedError.GetProviderErrorCode());
        Assert.Equal("Test error", mappedError.Message);
    }

    [Fact]
    public void GivenResultWithPaymentExecutionErrorAndProviderErrorCode_WhenMapToBadPaymentRequestError_ThenReturnMappedError()
    {
        // Arrange
        var baseError = new PaymentExecutionError("Test error", providerErrorCode: "provider_error_123");
        var result = Result.Fail(baseError);

        // Act
        var mappedError = ErrorMapper.MapToBadPaymentRequestError(result);

        // Assert
        Assert.Equal(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed, mappedError.GetErrorCode());
        Assert.Equal(ErrorType.BadPaymentRequest, mappedError.GetErrorType());
        Assert.Equal("provider_error_123", mappedError.GetProviderErrorCode());
        Assert.Equal("Test error", mappedError.Message);
    }


    [Fact]
    public void GivenResultWithoutPaymentExecutionError_WhenMapToBadPaymentRequestError_ThenThrowNotImplementedException()
    {
        var result = Result.Fail("Generic error");

        var act = () => ErrorMapper.MapToBadPaymentRequestError(result);

        Assert.Throws<NotImplementedException>(act);
    }

    [Fact]
    public void GivenResultWithoutPaymentExecutionError_WhenMapToGetProviderError_ThenReturnOriginalResult()
    {
        // Arrange
        var result = Result.Fail("Generic error");

        // Act
        var actual = ErrorMapper.MapToGetProviderError(result);

        // Assert
        actual.Should().Be(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public void GivenResultWithPaymentExecutionErrorAndNonTransientStatusCode_WhenMapToGetProviderError_ThenReturnFailedDependencyError(
        HttpStatusCode statusCode)
    {
        // Arrange
        var testError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(testError);

        // Act
        var mappedResult = ErrorMapper.MapToGetProviderError(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        mappedError.GetErrorType().Should().Be(ErrorType.FailedDependency);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(null)]
    public void GivenResultWithPaymentExecutionErrorAndTransientStatusCode_WhenMapToGetProviderError_ThenReturnDependencyTransientError(
        HttpStatusCode? statusCode)
    {
        // Arrange
        var baseError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(baseError);

        // Act
        var mappedResult = ErrorMapper.MapToGetProviderError(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        mappedError.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
    }

    [Fact]
    public void GivenResultWithoutPaymentExecutionError_WhenMapToGetProviderErrorForLambda_ThenReturnFailedDependencyError()
    {
        // Arrange
        var result = Result.Fail("Generic error");

        // Act
        var mappedResult = ErrorMapper.MapToGetProviderErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedResult.IsFailed.Should().BeTrue();
        mappedError.Should().NotBeNull();
        mappedError.GetErrorType().Should().Be(ErrorType.FailedDependency);
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        mappedError.Message.Should().Be("Failed to get provider state");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public void GivenResultWithPaymentExecutionErrorAndNonTransientStatusCode_WhenMapToGetProviderErrorForLambda_ThenReturnFailedDependencyError(
        HttpStatusCode statusCode)
    {
        // Arrange
        var testError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(testError);

        // Act
        var mappedResult = ErrorMapper.MapToGetProviderErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        mappedError.GetErrorType().Should().Be(ErrorType.FailedDependency);
        mappedError.Message.Should().Be("Test error");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(null)]
    public void GivenResultWithPaymentExecutionErrorAndTransientStatusCode_WhenMapToGetProviderErrorForLambda_ThenReturnDependencyTransientError(
        HttpStatusCode? statusCode)
    {
        // Arrange
        var baseError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(baseError);

        // Act
        var mappedResult = ErrorMapper.MapToGetProviderErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        mappedError.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        mappedError.Message.Should().Be("Test error");
    }

    [Fact]
    public void GivenResultWithoutPaymentExecutionError_WhenMapToCancelPaymentErrorForLambda_ThenReturnFailedDependencyError()
    {
        // Arrange
        var result = Result.Fail("Generic error");

        // Act
        var mappedResult = ErrorMapper.MapToCancelPaymentErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedResult.IsFailed.Should().BeTrue();
        mappedError.Should().NotBeNull();
        mappedError.GetErrorType().Should().Be(ErrorType.FailedDependency);
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        mappedError.Message.Should().Be("Failed to cancel payment with provider: Generic error");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public void GivenResultWithPaymentExecutionErrorAndNonTransientStatusCode_WhenMapToCancelPaymentErrorForLambda_ThenReturnFailedDependencyError(
        HttpStatusCode statusCode)
    {
        // Arrange
        var testError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(testError);

        // Act
        var mappedResult = ErrorMapper.MapToCancelPaymentErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        mappedError.GetErrorType().Should().Be(ErrorType.FailedDependency);
        mappedError.Message.Should().Be("Test error");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(null)]
    public void GivenResultWithPaymentExecutionErrorAndTransientStatusCode_WhenMapToCancelPaymentErrorForLambda_ThenReturnDependencyTransientError(
        HttpStatusCode? statusCode)
    {
        // Arrange
        var baseError = new PaymentExecutionError("Test error", httpStatusCode: statusCode);
        var result = Result.Fail(baseError);

        // Act
        var mappedResult = ErrorMapper.MapToCancelPaymentErrorForLambda(result);
        var mappedError = (PaymentExecutionError)mappedResult.Errors[0];

        // Assert
        mappedError.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        mappedError.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        mappedError.Message.Should().Be("Test error");
    }
}
