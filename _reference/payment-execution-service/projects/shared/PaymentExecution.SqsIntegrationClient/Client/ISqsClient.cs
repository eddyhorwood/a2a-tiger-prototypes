using Amazon.SQS.Model;

namespace PaymentExecution.SqsIntegrationClient.Client;

public interface ISqsClient
{
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest messageRequest);
    Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest receiveRequest, CancellationToken cancellationToken);
    Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest deleteRequest,
        CancellationToken cancellationToken);
}
