using Amazon.SQS;
using Amazon.SQS.Model;

namespace PaymentExecution.SqsIntegrationClient.Client;

public class SqsClient(IAmazonSQS client) : ISqsClient
{
    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest messageRequest)
    {
        var response = await client.SendMessageAsync(messageRequest);
        return response;
    }

    public async Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest receiveRequest, CancellationToken cancellationToken)
    {
        var response = await client.ReceiveMessageAsync(receiveRequest, cancellationToken);
        return response;
    }

    public async Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest deleteRequest,
        CancellationToken cancellationToken)
    {
        var response = await client.DeleteMessageBatchAsync(deleteRequest, cancellationToken);
        return response;
    }
}
