using System.Collections;
using Amazon.SQS.Model;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.UnitTests.TestData;

public class InvalidMessageBodyTestData : IEnumerable<object[]>
{
    private readonly List<object[]> _data = new()
    {

        new object[] { CreateMessageWithInvalidBody(TerminalStatus.Succeeded.ToString(), "00000000-0000-0000-0000-000000000000",
            Guid.NewGuid().ToString())},
        new object[] { CreateMessageWithInvalidBody(TerminalStatus.Failed.ToString(), "not-a-guid",
            Guid.NewGuid().ToString())},
        new object[] { CreateMessageWithInvalidBody(TerminalStatus.Succeeded.ToString(), Guid.NewGuid().ToString(),
            "not-a-guid")},
        new object[] { CreateMessageWithInvalidBody("not-a-valid-status", Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString())},
        new object[] {CreateMessageWithOmittedProperty("status")},
        new object[] {CreateMessageWithOmittedProperty("paymentRequestId")},
        new object[] {CreateMessageWithOmittedProperty("providerServiceId")}
    };

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string CreateInvalidBodyJsonString(string? status, string paymentRequestId, string providerServiceId)
    {
        return $@"
        {{
            ""Fee"":""5"",
            ""FeeCurrency"":""aud"",
            ""ProviderType"": ""Stripe"",
            ""ProviderTransactionReference"": ""MyTransActionRef"",
            ""Status"": ""{status}"",
            ""PaymentRequestId"":""{paymentRequestId}"",
            ""ProviderServiceId"":""{providerServiceId}""
          }}";
    }

    private static Message CreateMessageWithInvalidBody(string? status, string paymentRequestId, string providerServiceId)
    {
        return new Message()
        {
            MessageId = "message-id-invalid-message",
            ReceiptHandle = "receipt-handle-3",
            Body = CreateInvalidBodyJsonString(status, paymentRequestId, providerServiceId),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            {
                {
                    ExecutionConstants.XeroCorrelationId, new MessageAttributeValue()
                    {
                        DataType = "String",
                        StringValue = Guid.NewGuid().ToString()
                    }
                },
                {
                    ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                    {
                        DataType = "String",
                        StringValue = Guid.NewGuid().ToString()
                    }
                }
            }
        };
    }

    private static Message CreateMessageWithOmittedProperty(string propertyToOmit)
    {
        var message = propertyToOmit switch
        {
            "status" => new Message()
            {
                MessageId = "message-id-invalid-message",
                ReceiptHandle = "receipt-handle-3",
                Body = $@"
                        {{
                            ""Fee"":""5"",
                            ""FeeCurrency"":""aud"",
                            ""ProviderType"": ""Stripe"",
                            ""ProviderTransactionReference"": ""MyTransActionRef"",
                            ""PaymentRequestId"":""{Guid.NewGuid()}"",
                            ""ProviderServiceId"":""{Guid.NewGuid()}""
                          }}",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        ExecutionConstants.XeroCorrelationId,
                        new MessageAttributeValue() { DataType = "String.Guid", StringValue = Guid.NewGuid().ToString() }
                    },
                    {
                        ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                            {
                                DataType = "String",
                                StringValue = Guid.NewGuid().ToString()
                            }
                    }
                },

            },
            "paymentRequestId" => new Message()
            {
                MessageId = "message-id-invalid-message",
                ReceiptHandle = "receipt-handle-3",
                Body = $@"
                        {{
                            ""Fee"":""5"",
                            ""FeeCurrency"":""aud"",
                            ""ProviderType"": ""Stripe"",
                            ""ProviderTransactionReference"": ""MyTransActionRef"",
                            ""Status"": ""{TerminalStatus.Succeeded}"",
                            ""ProviderServiceId"":""{Guid.NewGuid()}""
                          }}",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        ExecutionConstants.XeroCorrelationId,
                        new MessageAttributeValue() { DataType = "String.Guid", StringValue = Guid.NewGuid().ToString() }
                    },
                    {
                    ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                    {
                        DataType = "String",
                        StringValue = Guid.NewGuid().ToString()
                    }
                }
                }
            },
            "providerServiceId" => new Message()
            {
                MessageId = "message-id-invalid-message",
                ReceiptHandle = "receipt-handle-3",
                Body = $@"
                        {{
                            ""Fee"":""5"",
                            ""FeeCurrency"":""aud"",
                            ""ProviderType"": ""Stripe"",
                            ""ProviderTransactionReference"": ""MyTransActionRef"",
                            ""Status"": ""{TerminalStatus.Succeeded}"",
                            ""PaymentRequestId"":""{Guid.NewGuid()}""
                          }}",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        ExecutionConstants.XeroCorrelationId,
                        new MessageAttributeValue() { DataType = "String.Guid", StringValue = Guid.NewGuid().ToString() }
                    },
                    {
                    ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                    {
                        DataType = "String",
                        StringValue = Guid.NewGuid().ToString()
                    }
                }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(propertyToOmit), propertyToOmit, null)
        };

        return message;
    }
}
