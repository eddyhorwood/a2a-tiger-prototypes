using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PaymentExecution.SqsIntegrationClient.Options;

namespace PaymentExecution.TestUtilities;

public class SqsUtility(
    IAmazonSQS sqsClient, 
    IOptions<ExecutionQueueOptions> executionSqsOptions,
    IOptions<CancelExecutionQueueOptions> cancelExecutionSqsOptions)
{
    private readonly ExecutionQueueOptions _executionQueueOptions = executionSqsOptions.Value;
    private readonly CancelExecutionQueueOptions _cancelExecutionQueueOptions = cancelExecutionSqsOptions.Value;
    private const int DefaultMaxMessages = 10;
    private const int DefaultLongPollingTimeout = 20;

    public async Task PurgeQueueAsync()
    {
        await sqsClient.PurgeQueueAsync(_executionQueueOptions.QueueUrl);
    }

    public async Task<ReceiveMessageResponse> ReceiveMessagesAsync(ReceiveMessageRequest? customReceiveMessageRequest = null)
    {
        var defaultReceiveMessageRequest = new ReceiveMessageRequest()
        {
            QueueUrl = _executionQueueOptions.QueueUrl,
            MaxNumberOfMessages = _executionQueueOptions.MaxNumberOfMessages != 0 ? _executionQueueOptions.MaxNumberOfMessages : DefaultMaxMessages,
            WaitTimeSeconds = _executionQueueOptions.LongPollingTimeoutSeconds != 0 ? _executionQueueOptions.LongPollingTimeoutSeconds : DefaultLongPollingTimeout,
        };

        var receiveMessageRequest = customReceiveMessageRequest ?? defaultReceiveMessageRequest;

        return await sqsClient.ReceiveMessageAsync(receiveMessageRequest, CancellationToken.None);
    }

    public async Task PurgeCancelQueueAsync()
    {
        await sqsClient.PurgeQueueAsync(_cancelExecutionQueueOptions.QueueUrl);
    }

    public async Task<ReceiveMessageResponse> ReceiveCancelMessagesAsync(int waitTimeSeconds = 2)
    {
        var receiveMessageRequest = new ReceiveMessageRequest()
        {
            QueueUrl = _cancelExecutionQueueOptions.QueueUrl,
            MaxNumberOfMessages = DefaultMaxMessages,
            WaitTimeSeconds = waitTimeSeconds,
            MessageAttributeNames = new List<string> { "All" }
        };

        return await sqsClient.ReceiveMessageAsync(receiveMessageRequest, CancellationToken.None);
    }
}
