using Amazon.Lambda.SQSEvents;
using static PaymentExecution.Domain.Constants.HttpHeaders;

namespace PaymentExecutionLambda.CancelLambda.Extensions;

public static class SqsMessageExtensions
{
    private static string? GetAttributeValue(this SQSEvent.SQSMessage message, string attributeName)
    {
        return message.MessageAttributes.TryGetValue(attributeName, out var attribute)
            ? attribute.StringValue
            : null;
    }

    public static string? GetTenantId(this SQSEvent.SQSMessage message)
    {
        return GetAttributeValue(message, XeroTenantId);
    }

    public static Guid GetCorrelationId(this SQSEvent.SQSMessage message)
    {
        var correlationIdString = GetAttributeValue(message, XeroCorrelationId);

        // Try to parse the correlation ID as a Guid, otherwise generate a new one
        if (!string.IsNullOrWhiteSpace(correlationIdString) && Guid.TryParse(correlationIdString, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }
}
