using System.Collections.Concurrent;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Commands;
using PaymentExecutionLambda.CancelLambda.Extensions;
using PaymentExecutionLambda.CancelLambda.Models;
using PaymentExecutionLambda.CancelLambda.Util;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace PaymentExecutionLambda.CancelLambda;

public class Function(ILogger<Function> logger, IServiceScopeFactory serviceScopeFactory, IMapper mapper)
{
    [LambdaFunction]
    public async Task<SQSBatchResponse> Handler(SQSEvent sqsEvent)
    {
        var batchItemFailures = new ConcurrentBag<SQSBatchResponse.BatchItemFailure>();
        var processingTasks = sqsEvent.Records.Select(async message =>
        {
            try
            {
                await ProcessMessageAsync(message, batchItemFailures);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing message {MessageId}. Message will be retried and sent to DLQ if max retries exceeded", message.MessageId);
                batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            }
        });

        await Task.WhenAll(processingTasks);
        return new SQSBatchResponse { BatchItemFailures = batchItemFailures.ToList() };
    }


    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ConcurrentBag<SQSBatchResponse.BatchItemFailure> batchItemFailures)
    {
        using var logScope = LoggingExtensions.PushContextProperties(message);
        using var serviceScope = serviceScopeFactory.CreateScope();
        var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();

        if (!TryParseCancelMessage(message, out var cancelPaymentMessage))
        {
            return;
        }

        var command = mapper.Map<ProcessCancelMessageCommand>(cancelPaymentMessage);
        var result = await mediator.Send(command);

        // Classify error and log decision with metadata
        var decision = LambdaErrorHandler.ClassifyError(result);
        LambdaErrorHandler.LogErrorDecision(logger, result, decision, message.MessageId);

        // Handle based on decision
        if (decision == ErrorHandlingDecision.RetryMessage)
        {
            batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
        }
        // Success or DeleteNonRetryableMessage: message will be automatically deleted from queue
    }

    private bool TryParseCancelMessage(SQSEvent.SQSMessage message, out CancelPaymentMessage parsedMessage)
    {
        parsedMessage = null!;
        var correlationId = message.GetCorrelationId();

        // Validate tenant ID
        var tenantIdString = message.GetTenantId();
        if (string.IsNullOrWhiteSpace(tenantIdString) || !Guid.TryParse(tenantIdString, out var tenantId))
        {
            logger.LogError("Invalid or missing tenant ID '{TenantId}' for request {MessageBody}. Message will be deleted as it is not retryable", tenantIdString, message.Body);
            return false;
        }

        // Deserialize request
        if (!JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(message.Body, out var cancelPaymentRequest)
            || cancelPaymentRequest == null)
        {
            logger.LogError("Failed to deserialize cancel message for request {MessageBody}. Message will be deleted as it is not retryable", message.Body);
            return false;
        }

        parsedMessage = new CancelPaymentMessage(tenantId, correlationId, cancelPaymentRequest);
        return true;
    }
}
