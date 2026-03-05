using FluentResults;

namespace PaymentExecution.Common.UnitTests.Extensions;

public class ResultExtensionTests
{

    [Fact]
    public void GivenResultWithErrors_WhenGetFirstErrorMessage_ThenReturnFirstErrorMessage()
    {
        // Arrange
        var result = Result.Fail("First error").WithError("Second error");

        // Act
        var errorMessage = result.GetFirstErrorMessage();

        // Assert
        Assert.Equal("First error", errorMessage);
    }

    [Fact]
    public void GivenResultWithoutErrors_WhenGetFirstErrorMessage_ThenReturnEmptyString()
    {
        // Arrange
        var result = Result.Ok();

        // Act
        var errorMessage = result.GetFirstErrorMessage();

        // Assert
        Assert.Equal(string.Empty, errorMessage);
    }

    [Fact]
    public void GivenResultWithNullErrorMessage_WhenGetFirstErrorMessage_ThenReturnEmptyString()
    {
        // Arrange
        var result = Result.Fail((string)null!);

        // Act
        var errorMessage = result.GetFirstErrorMessage();

        // Assert
        Assert.Equal(string.Empty, errorMessage);
    }
}
