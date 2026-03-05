using System.Text.Json;
using Amazon.SQS.Model;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentExecution.Common;
using PaymentExecution.SqsIntegrationClient.Client;
using PaymentExecution.SqsIntegrationClient.Options;
namespace PaymentExecution.SqsIntegrationClient.Service;

public class CancelExecutionQueueService(
    IOptions<CancelExecutionQueueOptions> config,
    ISqsClient sqsClient,
    ILogger<CancelExecutionQueueService> logger) : ICancelExecutionQueueService
{
    public async Task<Result> SendMessageAsync<T>(T message, int delaySeconds, string xeroCorrelationId, string xeroTenantId)
    {
        try
        {
            SendMessageRequest messageRequest = new()
            {
                QueueUrl = config.Value.QueueUrl,
                DelaySeconds = delaySeconds,
                MessageBody = JsonSerializer.Serialize(message),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        ExecutionConstants.XeroCorrelationId, new MessageAttributeValue
                        {
                            DataType = "String", StringValue = xeroCorrelationId
                        }
                    },
                    {
                        ExecutionConstants.XeroTenantId, new MessageAttributeValue
                        {
                            DataType = "String", StringValue = xeroTenantId
                        }
                    }
                }
            };

            await sqsClient.SendMessageAsync(messageRequest);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to cancel execution queue");
            return Result.Fail(new PaymentExecutionError(ErrorConstants.ErrorMessage.SendMessageToCancelExecutionQueueError));
        }
    }
}
