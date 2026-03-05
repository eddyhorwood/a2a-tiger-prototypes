using FluentAssertions;
using PaymentExecution.Common.Models;
namespace PaymentExecution.Common.UnitTests.Models;

public class RedactedExceptionTests
{
    [Fact]
    public void GivenValidMessageAndExceptionType_WhenRedactedExceptionCreated_ThenPropertiesSetCorrectly()
    {
        // Arrange
        var message = "Test error message";
        var exceptionType = ExceptionType.DatabaseException;

        // Act
        var redactedException = new RedactedException(message, exceptionType);

        // Assert
        redactedException.Message.Should().Be("Test error message Original Exception Type: DatabaseException");
        redactedException.InnerException.Should().BeNull();
    }

    [Fact]
    public void GivenValidMessageExceptionTypeAndInnerException_WhenRedactedExceptionCreated_ThenAllPropertiesSetCorrectly()
    {
        // Arrange
        var message = "Test error message";
        var exceptionType = ExceptionType.DatabaseException;
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var redactedException = new RedactedException(message, exceptionType, innerException);

        // Assert
        redactedException.Message.Should().Be("Test error message Original Exception Type: DatabaseException");
        redactedException.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void GivenMessageContainingEmailAddress_WhenRedactedExceptionCreated_ThenEmailAddressSanitized()
    {
        // Arrange
        var message = "Error occurred for user john.doe@example.com";
        var exceptionType = ExceptionType.DatabaseException;

        // Act
        var redactedException = new RedactedException(message, exceptionType);

        // Assert
        redactedException.Message.Should().Be("Error occurred for user ***MASKED*** Original Exception Type: DatabaseException");
    }

    [Fact]
    public void GivenEmptyMessage_WhenRedactedExceptionCreated_ThenEmptyMessageHandledCorrectly()
    {
        // Arrange
        var message = "";
        var exceptionType = ExceptionType.DatabaseException;

        // Act
        var redactedException = new RedactedException(message, exceptionType);

        // Assert
        redactedException.Message.Should().Be(" Original Exception Type: DatabaseException");
    }
}

