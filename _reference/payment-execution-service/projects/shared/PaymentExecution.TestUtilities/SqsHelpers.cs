using Amazon.Lambda.SQSEvents;

namespace PaymentExecution.TestUtilities;

public static class SqsHelpers
{
    /// <summary>
    /// Creates an SQSEvent with a single message containing the specified body and attributes
    /// </summary>
    /// <param name="messageBody">The message body content (typically JSON)</param>
    /// <param name="tenantId">The Xero-Tenant-Id value</param>
    /// <param name="correlationId">The Xero-Correlation-Id value</param>
    /// <returns>A properly formatted SQSEvent for Lambda testing</returns>
    public static SQSEvent CreateSqsEvent(
        string messageBody,
        string? tenantId = null,
        string? correlationId = null)
    {
        var messageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>();

        if (!string.IsNullOrEmpty(tenantId))
        {
            messageAttributes.Add("Xero-Tenant-Id", new SQSEvent.MessageAttribute
            {
                StringValue = tenantId,
                DataType = "String"
            });
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            messageAttributes.Add("Xero-Correlation-Id", new SQSEvent.MessageAttribute
            {
                StringValue = correlationId,
                DataType = "String"
            });
        }

        return new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Body = messageBody,
                    MessageAttributes = messageAttributes,
                    ReceiptHandle = Guid.NewGuid().ToString()
                }
            ]
        };
    }

    /// <summary>
    /// Creates an SQSEvent with multiple messages
    /// </summary>
    /// <param name="messages">Collection of tuples containing (messageBody, tenantId, correlationId)</param>
    /// <returns>An SQSEvent with multiple records</returns>
    public static SQSEvent CreateSqsEventWithMultipleMessages(
        IEnumerable<(string messageBody, string tenantId, string correlationId)> messages)
    {
        var records = messages.Select(msg =>
        {
            var messageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                { "Xero-Tenant-Id", new SQSEvent.MessageAttribute { StringValue = msg.tenantId, DataType = "String" } },
                { "Xero-Correlation-Id", new SQSEvent.MessageAttribute { StringValue = msg.correlationId, DataType = "String" } }
            };

            return new SQSEvent.SQSMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = msg.messageBody,
                MessageAttributes = messageAttributes,
                ReceiptHandle = Guid.NewGuid().ToString()
            };
        }).ToList();

        return new SQSEvent
        {
            Records = records
        };
    }
}

