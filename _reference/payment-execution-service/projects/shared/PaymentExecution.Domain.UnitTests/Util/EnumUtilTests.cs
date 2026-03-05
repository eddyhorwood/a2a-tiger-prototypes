using System.Runtime.Serialization;
using FluentAssertions;
using PaymentExecution.Domain.Util;

namespace PaymentExecution.Domain.UnitTests.Util;

public class EnumUtilTests
{
    private enum TestEnumData
    {
        [EnumMember(Value = "has_attribute")]
        HasAttribute,
        NoAttribute
    }

    [Fact]
    public void GivenEnumMemberWithEnumMemberAttribute_WhenGetEnumMemberValue_ThenReturnsAttributeValue()
    {
        // Arrange
        var testEnumValue = TestEnumData.HasAttribute;

        // Act
        var result = EnumUtil.GetEnumMemberValue(testEnumValue);

        // Assert
        result.Should().Be("has_attribute");
    }

    [Fact]
    public void GivenEnumMemberWithoutEnumMemberAttribute_WhenGetEnumMemberValue_ThenReturnsToStringValue()
    {
        // Arrange
        var testEnumValue = TestEnumData.NoAttribute;

        // Act
        var result = EnumUtil.GetEnumMemberValue(testEnumValue);

        // Assert
        result.Should().Be("NoAttribute");
    }

    [Fact]
    public void GivenUndefinedEnumMemberEnumMember_WhenGetEnumMemberValue_ThenReturnsToStringValue()
    {
        // Arrange
        var testEnumValue = (TestEnumData)999;

        // Act
        var result = EnumUtil.GetEnumMemberValue(testEnumValue);

        // Assert
        result.Should().Be("999");
    }
}
