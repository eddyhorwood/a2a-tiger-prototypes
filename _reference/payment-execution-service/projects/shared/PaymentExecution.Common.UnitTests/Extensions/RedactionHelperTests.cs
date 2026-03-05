using System.Text.RegularExpressions;
using FluentAssertions;
using Moq;
using PaymentExecution.Common.Extensions;
namespace PaymentExecution.Common.UnitTests.Extensions;

public class RedactionHelperTests : IDisposable
{
    private readonly IRegexWrapper _originalRegexWrapper = RedactionHelper.RegexWrapper;

    public void Dispose()
    {
        RedactionHelper.RegexWrapper = _originalRegexWrapper;
        GC.SuppressFinalize(this);
    }


    [Fact]
    public void GivenMessageWithSingleEmail_WhenSanitizeMessageCalled_ThenEmailReplacedWithMasked()
    {
        // Arrange
        var message = "User john.doe@example.com encountered an error";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be("User ***MASKED*** encountered an error");
    }

    [Fact]
    public void GivenMessageWithMultipleEmails_WhenSanitizeMessageCalled_ThenAllEmailsReplacedWithMasked()
    {
        // Arrange
        var message = "Communication between admin@company.com and user@client.org failed";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be("Communication between ***MASKED*** and ***MASKED*** failed");
    }

    [Fact]
    public void GivenMessageWithNoEmails_WhenSanitizeMessageCalled_ThenOriginalMessageReturned()
    {
        // Arrange
        var message = "This is a simple error message without any emails";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be("This is a simple error message without any emails");
    }

    [Fact]
    public void GivenNullMessage_WhenSanitizeMessageCalled_ThenNullReturned()
    {
        // Arrange
        string? message = null;

        // Act
        var result = RedactionHelper.SanitizeMessage(message!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenEmptyMessage_WhenSanitizeMessageCalled_ThenEmptyStringReturned()
    {
        // Arrange
        var message = "";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("admin+tag@company.org")]
    [InlineData("user_123@sub.domain.net")]
    [InlineData("a@b.co")]
    [InlineData("very.long.email.address@very.long.domain.name.com")]
    [InlineData("user@domain")]
    public void GivenMessageWithVariousEmailFormats_WhenSanitizeMessageCalled_ThenEmailsSanitizedCorrectly(string email)
    {
        // Arrange
        var message = $"Error occurred for {email} during processing";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be("Error occurred for ***MASKED*** during processing");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user.domain.com")]
    public void GivenMessageWithInvalidEmailFormats_WhenSanitizeMessageCalled_ThenMessageNotSanitized(string invalidEmail)
    {
        // Arrange
        var message = $"Error occurred for {invalidEmail} during processing";

        // Act
        var result = RedactionHelper.SanitizeMessage(message);

        // Assert
        result.Should().Be(message); // Should remain unchanged
    }

    [Fact]
    public void GivenRegexWrapperTimeOut_WhenSanitizeMessage_ThenReturnsExpectedMessage()
    {
        //Arrange
        var inputMessage = "This is a test message with a complicated pattern that will cause a timeout.";
        var mockRegexWrapper = new Mock<IRegexWrapper>();

        mockRegexWrapper.Setup(r => r.Replace(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RegexOptions>(), It.IsAny<TimeSpan>()))
            .Throws(new RegexMatchTimeoutException("Regex match timed out"));

        //Act 
        RedactionHelper.RegexWrapper = mockRegexWrapper.Object;
        var result = RedactionHelper.SanitizeMessage(inputMessage);

        //Assert
        result.Should().Be(inputMessage + " Error sanitizing message due to timeout.");
    }
}
