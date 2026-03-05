using Amazon.Lambda.SQSEvents;
using AutoFixture.Xunit2;
using FluentAssertions;
using PaymentExecutionLambda.CancelLambda.Extensions;

namespace PaymentExecutionLambda.CancelLambdaUnitTests.Extensions;

public class LoggingExtensionsTests
{
    [Theory]
    [AutoData]
    public void GivenValidSqsMessage_WhenPushContextPropertiesCalled_ThenReturnsDisposable(
        string messageId,
        string correlationId,
        string tenantId,
        string body)
    {
        // Arrange
        var sqsMessage = CreateSqsMessage(messageId, correlationId, tenantId, body);

        // Act
        var result = LoggingExtensions.PushContextProperties(sqsMessage);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IDisposable>();

        // Clean up
        result.Dispose();
    }

    [Theory]
    [AutoData]
    public void GivenSqsMessageWithNullTenantId_WhenPushContextPropertiesCalled_ThenDoesNotThrow(
        string messageId,
        string correlationId,
        string body)
    {
        // Arrange
        var sqsMessage = CreateSqsMessage(messageId, correlationId, null, body);

        // Act & Assert - Should not throw any exceptions
        var act = () =>
        {
            using var context = LoggingExtensions.PushContextProperties(sqsMessage);
            return context;
        };

        act.Should().NotThrow();
    }

    [Theory]
    [AutoData]
    public void GivenSqsMessageWithMissingCorrelationId_WhenPushContextPropertiesCalled_ThenDoesNotThrow(
        string messageId,
        string tenantId,
        string body)
    {
        // Arrange
        var sqsMessage = CreateSqsMessage(messageId, null, tenantId, body);

        // Act & Assert 
        var act = () =>
        {
            using var context = LoggingExtensions.PushContextProperties(sqsMessage);
            return context;
        };

        act.Should().NotThrow();
    }

    private static SQSEvent.SQSMessage CreateSqsMessage(string messageId, string? correlationId, string? tenantId, string body)
    {
        var sqsMessage = new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = body,
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>()
        };

        if (correlationId != null)
        {
            sqsMessage.MessageAttributes["XeroCorrelationId"] = new SQSEvent.MessageAttribute
            {
                StringValue = correlationId
            };
        }

        if (tenantId != null)
        {
            sqsMessage.MessageAttributes["XeroTenantId"] = new SQSEvent.MessageAttribute
            {
                StringValue = tenantId
            };
        }

        return sqsMessage;
    }
}
