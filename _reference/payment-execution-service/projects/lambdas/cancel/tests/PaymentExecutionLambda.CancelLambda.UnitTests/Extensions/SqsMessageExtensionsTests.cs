using Amazon.Lambda.SQSEvents;
using AutoFixture.Xunit2;
using FluentAssertions;
using PaymentExecutionLambda.CancelLambda.Extensions;
using static PaymentExecution.Domain.Constants.HttpHeaders;

namespace PaymentExecutionLambda.CancelLambdaUnitTests.Extensions;

public class SqsMessageExtensionsTests
{
    [Theory, AutoData]
    public void GivenMessageHasXeroTenantIdAttribute_WhenGetTenantCalled_ReturnsAttributeValue(string tenantId)
    {
        // Arrange
        var message = CreateSqsMessage(XeroTenantId, tenantId);

        // Act
        var result = message.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GivenMessageDoesNotHaveXeroTenantIdAttribute_WhenGetTenantCalled_ReturnNull()
    {
        // Arrange
        var message = new SQSEvent.SQSMessage
        {
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>()
        };

        // Act
        var result = message.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenMessageHasEmptyXeroTenantIdAttribute_WhenGetTenantCalled_ShouldReturnEmptyString()
    {
        // Arrange
        var message = CreateSqsMessage(XeroTenantId, string.Empty);

        // Act
        var result = message.GetTenantId();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void GivenMessageHasXeroCorrelationIdAttribute_WhenGetCorrelationIdCalled_ReturnAttributeValue()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var message = CreateSqsMessage(XeroCorrelationId, correlationId.ToString());

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().Be(correlationId);
    }

    [Fact]
    public void GivenMessageDoesNotHaveXeroCorrelationIdAttribute_WhenGetCorrelationIdCalled_ReturnNewGuid()
    {
        // Arrange
        var message = new SQSEvent.SQSMessage
        {
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>()
        };

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GivenMessageHasEmptyXeroCorrelationIdAttribute_WhenGetCorrelationId_ReturnNewGuid()
    {
        // Arrange
        var message = CreateSqsMessage(XeroCorrelationId, string.Empty);

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GivenMessageHasWhitespaceXeroCorrelationIdAttribute_WhenGetCorrelationIdCalled_ReturnNewGuid()
    {
        // Arrange
        var message = CreateSqsMessage(XeroCorrelationId, "   ");

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GivenMessageHasInvalidGuidXeroCorrelationIdAttribute_WhenGetCorrelationIdCalled_ReturnNewGuid()
    {
        // Arrange
        var message = CreateSqsMessage(XeroCorrelationId, "not-a-valid-guid");

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GivenMessageHasNullXeroCorrelationIdAttribute_WhenGetCorrelationIdCalled_ReturnNewGuid()
    {
        // Arrange
        var message = new SQSEvent.SQSMessage
        {
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                [XeroCorrelationId] = new SQSEvent.MessageAttribute
                {
                    StringValue = null
                }
            }
        };

        // Act
        var result = message.GetCorrelationId();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    private static SQSEvent.SQSMessage CreateSqsMessage(string attributeName, string attributeValue)
    {
        return new SQSEvent.SQSMessage
        {
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                [attributeName] = new SQSEvent.MessageAttribute
                {
                    StringValue = attributeValue
                }
            }
        };
    }
}
