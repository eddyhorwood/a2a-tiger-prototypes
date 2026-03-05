namespace PaymentExecution.Common.UnitTests;

public class PaymentExecutionErrorTests
{
    [Fact]
    public void GivenErrorWithNoErrorType_WhenGetErrorType_ThenReturnsNull()
    {
        var baseError = new PaymentExecutionError("some-message", "some-provider-error-message");

        var errorType = baseError.GetErrorType();

        Assert.Null(errorType);
    }

    [Fact]
    public void GivenErrorWithErrorType_WhenGetErrorType_ThenReturnsExpectedErrorType()
    {
        var expectedErrorType = ErrorType.ClientError;
        var baseError = new PaymentExecutionError("some-message", expectedErrorType, "some-error-code");

        var errorType = baseError.GetErrorType();

        Assert.Equal(expectedErrorType, errorType);
    }

    [Fact]
    public void GivenErrorWithNoErrorCode_WhenGetErrorCode_ThenReturnsEmptyString()
    {
        var baseError = new PaymentExecutionError("some-message", "some-provider-error-message");

        var errorCode = baseError.GetErrorCode();

        Assert.Equal(string.Empty, errorCode);
    }

    [Fact]
    public void GivenErrorWithErrorCode_WhenGetErrorCode_ThenReturnsExpectedErrorCode()
    {
        var expectedErrorCode = "expected-error-code";
        var baseError = new PaymentExecutionError("some-message", ErrorType.PaymentFailed, expectedErrorCode);

        var errorCode = baseError.GetErrorCode();

        Assert.Equal(expectedErrorCode, errorCode);
    }


    [Fact]
    public void GivenErrorWithErrorCode_WhenGetProviderErrorCode_ThenReturnsExpectedProviderErrorCode()
    {
        var expectedProviderErrorCode = "some-provider-error-message";
        var baseError = new PaymentExecutionError("some-message", expectedProviderErrorCode);

        var providerErrorCode = baseError.GetProviderErrorCode();

        Assert.Equal(expectedProviderErrorCode, providerErrorCode);
    }

    [Fact]
    public void GivenValidErrorCode_WhenSetErrorCode_ThenSetsErrorCodeAsExpected()
    {
        var expectedErrorCode = "some-provider-error-message";
        var baseError = new PaymentExecutionError("some-message", "some-provider-error-message");

        baseError.SetErrorCode(expectedErrorCode);

        Assert.Equal(expectedErrorCode, baseError.Metadata[ErrorMetadataKey.ErrorCode]);
    }

    [Fact]
    public void GivenValidErrorType_WhenSetErrorType_ThenSetsErrorTypeAsExpected()
    {
        var expectedErrorType = ErrorType.ClientError;
        var baseError = new PaymentExecutionError("some-message", expectedErrorType, "some-error-code");

        baseError.SetErrorType(expectedErrorType);

        Assert.Equal(expectedErrorType, baseError.Metadata[ErrorMetadataKey.ErrorType]);
    }
}
