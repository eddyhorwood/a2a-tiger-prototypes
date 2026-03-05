using Amazon.SQS.Model;
using Microsoft.Extensions.Primitives;

namespace PaymentExecution.SqsIntegrationClient.Service;

public interface IExecutionQueueService
{
    Task SendMessageAsync<T>(T message, StringValues xeroCorrelationId, StringValues xeroTenantId);
    Task<ReceiveMessageResponse> GetMessagesAsync(CancellationToken cancellationToken);
    Task<DeleteMessageBatchResponse> DeleteMessagesAsync(List<Message> messagesToDelete,
        CancellationToken cancellationToken);

    void SetNewRelicDistributedTraceData(SendMessageRequest messageRequest, string key, string value);
    IEnumerable<string> AcceptNewRelicDistributedTraceData(Message message, string key);
}
