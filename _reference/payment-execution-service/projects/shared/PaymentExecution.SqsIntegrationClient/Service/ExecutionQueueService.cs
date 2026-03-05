using System.Text.Json;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PaymentExecution.Common;
using PaymentExecution.NewRelicClient;
using PaymentExecution.SqsIntegrationClient.Client;
using PaymentExecution.SqsIntegrationClient.Options;

namespace PaymentExecution.SqsIntegrationClient.Service;

public class ExecutionQueueService(
    IOptions<ExecutionQueueOptions> config,
    ILogger<ExecutionQueueService> logger,
    ISqsClient sqsClient,
    IMonitoringClient monitoringService) : IExecutionQueueService
{
    public async Task SendMessageAsync<T>(T message, StringValues xeroCorrelationId, StringValues xeroTenantId)
    {
        SendMessageRequest messageRequest = new()
        {
            QueueUrl = config.Value.QueueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    ExecutionConstants.XeroCorrelationId,
                    new MessageAttributeValue { DataType = "String", StringValue = xeroCorrelationId }
                },
                {
                    ExecutionConstants.XeroTenantId,
                    new MessageAttributeValue { DataType = "String", StringValue = xeroTenantId }
                }
            }
        };

        monitoringService.InsertDistributedTraceHeaders(messageRequest, SetNewRelicDistributedTraceData);

        var response = await sqsClient.SendMessageAsync(messageRequest);

        var paymentRequestId = typeof(T).GetProperty("PaymentRequestId")!.GetValue(message)?.ToString();
        logger.LogDebug(
            "Successfully sent message to queue: {QueueUrl} with message id: {MessageId}. PaymentRequestId: {PaymentRequestId}",
            messageRequest.QueueUrl, response.MessageId, paymentRequestId);
    }

    public async Task<ReceiveMessageResponse> GetMessagesAsync(CancellationToken cancellationToken)
    {
        ReceiveMessageRequest receiveMessageRequest = new()
        {
            QueueUrl = config.Value.QueueUrl,
            MaxNumberOfMessages = config.Value.MaxNumberOfMessages,
            WaitTimeSeconds = config.Value.LongPollingTimeoutSeconds,
            MessageAttributeNames =
                new List<string>() { ExecutionConstants.XeroCorrelationId, ExecutionConstants.XeroTenantId }
                    .Concat(ExecutionConstants.NewRelicConstants.DistributedTracingHeaders).ToList()
        };

        var messages = await sqsClient.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
        logger.LogDebug("Received {MessageCount} messages from queue", messages.Messages.Count);
        return messages;
    }

    public async Task<DeleteMessageBatchResponse> DeleteMessagesAsync(List<Message> messagesToDelete,
        CancellationToken cancellationToken)
    {
        var deleteMessageEntries = messagesToDelete.Select(m => new DeleteMessageBatchRequestEntry()
        {
            Id = m.MessageId,
            ReceiptHandle = m.ReceiptHandle
        }).ToList();

        var deleteMessageBatchRequest = new DeleteMessageBatchRequest
        {
            QueueUrl = config.Value.QueueUrl,
            Entries = deleteMessageEntries,
        };

        var result = await sqsClient.DeleteMessageBatchAsync(deleteMessageBatchRequest, cancellationToken);
        return result;
    }

    public void SetNewRelicDistributedTraceData(SendMessageRequest messageRequest, string key, string value)
    {
        messageRequest.MessageAttributes?.TryAdd(key,
            new MessageAttributeValue { DataType = "String", StringValue = value });
    }

    // See https://docs.newrelic.com/docs/apm/agents/net-agent/net-agent-api/net-agent-api/#ITransaction
    // for more information on distributed tracing and why this method signature is used
    public IEnumerable<string> AcceptNewRelicDistributedTraceData(Message message, string key)
    {
        var data = new List<string>();
        if (message.MessageAttributes == null)
        {
            return data;
        }

        if (message.MessageAttributes.TryGetValue(key, out var value) && value != null)
        {
            data.Add(value.StringValue);
        }

        return data;
    }
}
