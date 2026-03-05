using System.Text.RegularExpressions;
using FluentAssertions;
using PaymentExecution.Common.Extensions;
namespace PaymentExecution.Common.UnitTests.Extensions;

public class RegexWrapperTests
{
    private readonly RegexWrapper _regexWrapper = new();

    [Theory]
    [InlineData("Contact us at test@example.com or admin@company.org", "Contact us at ***MASKED*** or ***MASKED***")]
    [InlineData("Reach out to support@gmail for assistance", "Reach out to ***MASKED*** for assistance")]
    [InlineData("Contact admin@company and support@help.com for assistance", "Contact ***MASKED*** and ***MASKED*** for assistance")]
    public void GivenValidEmailPattern_WhenReplaceCalled_ThenShouldReplaceCorrectly(string inputString, string outputString)
    {
        // Arrange
        var pattern = RedactionHelper.EmailPattern;
        var replacement = "***MASKED***";
        var options = RegexOptions.IgnoreCase;
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = _regexWrapper.Replace(inputString, pattern, replacement, options, timeout);

        // Assert
        result.Should().Be(outputString);
    }

    [Fact]
    public void GivenNoMatches_WhenReplaceCalled_ThenShouldReturnOriginalString()
    {
        // Arrange
        var input = "This is a test without any emails";
        var pattern = RedactionHelper.EmailPattern;
        var replacement = "***MASKED***";
        var options = RegexOptions.IgnoreCase;
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = _regexWrapper.Replace(input, pattern, replacement, options, timeout);

        // Assert
        result.Should().Be(input);
    }
}
