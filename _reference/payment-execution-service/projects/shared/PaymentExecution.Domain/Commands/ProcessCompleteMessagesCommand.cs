using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.SQS.Model;
using AutoMapper;
using CollectingPayments.Common.Domain.Headers;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using PaymentExecution.Common;
using PaymentExecution.Common.Models;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.NewRelicClient;
using PaymentExecution.Repository;
using PaymentExecution.SqsIntegrationClient.Service;

namespace PaymentExecution.Domain.Commands;

public class ProcessCompleteMessagesCommand : IRequest<ProcessCompleteMessagesCommandResponse>, ISkipLoggingBehavior;

public class ProcessCompleteMessagesCommandResponse
{
    public bool IsProcessingError { get; init; }
    public int SuccessfullyProcessedMessages { get; init; }
}

public class
    ProcessCompleteMessagesCommandHandler(
        IExecutionQueueService executionQueueService,
        IMonitoringClient monitoringClient,
        IPaymentTransactionRepository paymentTransactionRepository,
        ILogger<ProcessCompleteMessagesCommandHandler> logger,
        IProcessCompleteMessageDomainService processCompleteMessageDomainService,
        IMapper mapper)
    : IRequestHandler<ProcessCompleteMessagesCommand,
        ProcessCompleteMessagesCommandResponse>
{

    public async Task<ProcessCompleteMessagesCommandResponse> Handle(ProcessCompleteMessagesCommand request, CancellationToken cancellationToken)
    {
        var getMessageResponse = await executionQueueService.GetMessagesAsync(cancellationToken);

        if (getMessageResponse.Messages.Count == 0)
        {
            return new ProcessCompleteMessagesCommandResponse() { IsProcessingError = false };
        }

        var successfullyProcessedMessages = new ConcurrentBag<Message>();

        var processingTasks = getMessageResponse.Messages.Select(async message =>
        {
            await ProcessSingleMessage(message, successfullyProcessedMessages, cancellationToken);
        });

        await Task.WhenAll(processingTasks);

        if (successfullyProcessedMessages.IsEmpty)
        {
            logger.LogWarning("No messages have been successfully processed.");
            return new ProcessCompleteMessagesCommandResponse()
            {
                IsProcessingError = true
            };
        }

        var response = await HandleBatchDeleteMessages(successfullyProcessedMessages.ToList(), cancellationToken);
        logger.LogInformation("Successfully processed {SuccessfullyProcessedMessages} messages", response.SuccessfullyProcessedMessages);

        return response;
    }

    [Transaction]
    private async Task ProcessSingleMessage(Message message,
        ConcurrentBag<Message> successfullyProcessedMessages, CancellationToken cancellationToken)
    {
        var messageAttributeValue = GetOrAssignCorrelationIdMessageAttribute(message);
        monitoringClient.AcceptDistributedTraceHeaders(message, executionQueueService.AcceptNewRelicDistributedTraceData, TransportType.Queue);

        var logTags = new Dictionary<string, string>
        {
            { XeroSoaHeaders.CorrelationId, messageAttributeValue.StringValue }
        };
        using (logger.BeginScope(logTags))
        {
            try
            {
                var processingResult = await ProcessMessage(message, cancellationToken);

                if (processingResult.IsSuccess)
                {
                    successfullyProcessedMessages.Add(message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing message {MessageId}", message.MessageId);
            }
        }
    }

    private MessageAttributeValue GetOrAssignCorrelationIdMessageAttribute(Message message)
    {
        if (!message.MessageAttributes.TryGetValue(XeroSoaHeaders.CorrelationId, out var messageAttributeValue))
        {
            messageAttributeValue = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = Guid.NewGuid().ToString()
            };
            message.MessageAttributes.Add(XeroSoaHeaders.CorrelationId, messageAttributeValue);
            logger.LogInformation("No Xero-Correlation-Id was found for Message with ID {Message}. New Xero-Correlation-Id added.", message.MessageId);
        }

        return messageAttributeValue;
    }

    private async Task<Result> ProcessMessage(Message message, CancellationToken cancellationToken)
    {
        var transformedMessageResult = TransformMessageToDomain(message);
        if (transformedMessageResult.IsFailed || cancellationToken.IsCancellationRequested)
        {
            return transformedMessageResult.ToResult();
        }

        var domainMessage = transformedMessageResult.Value;
        var getPaymentTransactionResult =
            await paymentTransactionRepository.GetPaymentTransactionsByPaymentRequestId(domainMessage.PaymentRequestId);
        if (getPaymentTransactionResult.IsFailed)
        {
            return getPaymentTransactionResult.ToResult();
        }

        var paymentTransactionRecordFromDb = getPaymentTransactionResult.Value;
        if (paymentTransactionRecordFromDb == null)
        {
            logger.LogCritical("No database record found for PaymentRequestId. PaymentRequestId: {PaymentRequestId}", domainMessage.PaymentRequestId);
            return Result.Fail("No database record found for PaymentRequestId");
        }

        var errorStateResult = processCompleteMessageDomainService.EvaluateIfRecordIsInErrorState(paymentTransactionRecordFromDb);
        if (errorStateResult.IsFailed)
        {
            return errorStateResult;
        }

        var eventShouldBeIgnored = processCompleteMessageDomainService.ShouldEventBeIgnored(paymentTransactionRecordFromDb, domainMessage);
        if (eventShouldBeIgnored)
        {
            return Result.Ok();
        }

        if (!Enum.TryParse<StripeValidCompleteStatus>(domainMessage.Status, out var parsedStatus))
        {
            logger.LogInformation("No call to payment request required. Message is not of a valid complete status. MessageId: {MessageId}, Status: {Status}",
                message.MessageId, domainMessage.Status);
            return Result.Ok();
        }

        var updatePaymentTransactionResult = await processCompleteMessageDomainService.HandleUpdateDbAsync(domainMessage, parsedStatus);
        if (updatePaymentTransactionResult.IsFailed)
        {
            return updatePaymentTransactionResult;
        }

        var updatePaymentRequestResult = await UpdatePaymentRequestAsync(parsedStatus, domainMessage);
        return updatePaymentRequestResult;
    }

    private Result<CompleteMessage> TransformMessageToDomain(Message message)
    {
        try
        {
            var messageBody = JsonSerializer.Deserialize<CompleteMessageBody>(message.Body);

            if (!message.MessageAttributes.TryGetValue(ExecutionConstants.XeroTenantId, out var xeroTenantId))
            {
                if (xeroTenantId == null || xeroTenantId.StringValue == null)
                {
                    logger.LogError("Event does not have tenantId which is a required property for MessageId:{MessageId}, PaymentRequestId:{PaymentRequestId}, ProviderServiceId: {ProviderServiceId}",
                        message.MessageId, messageBody!.PaymentRequestId, messageBody.ProviderServiceId);
                    return Result.Fail("No Xero-Tenant-Id attribute found on message being processed");
                }
            }

            var completeMessage = mapper.Map<CompleteMessage>(messageBody);

            completeMessage.XeroCorrelationId = message.MessageAttributes[ExecutionConstants.XeroCorrelationId].StringValue ?? Guid.NewGuid().ToString();
            completeMessage.XeroTenantId = xeroTenantId.StringValue;
            completeMessage.MessageId = message.MessageId;
            completeMessage.ReceiptHandle = message.ReceiptHandle;

            return completeMessage;
        }
        catch (Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.TransformMessageException);
            logger.LogError(redactedException, "An error occured attempting to transform message to domain. MessageId: {MessageId}",
                message.MessageId);
            return Result.Fail("Error transforming message to domain");
        }

    }

    private async Task<Result> UpdatePaymentRequestAsync(StripeValidCompleteStatus parsedStatus, CompleteMessage domainMessage)
    {
        switch (parsedStatus)
        {
            case StripeValidCompleteStatus.Succeeded:
                var updateToExecutionSucceed = await processCompleteMessageDomainService.HandleExecutionSucceedPaymentRequestAsync(domainMessage);
                return updateToExecutionSucceed;
            case StripeValidCompleteStatus.Failed:
                var updateToFailResult = await processCompleteMessageDomainService.HandleFailPaymentRequestAsync(domainMessage);
                return updateToFailResult;
            case StripeValidCompleteStatus.Cancelled:
                var updateToCancelResult = await processCompleteMessageDomainService.HandleCancelPaymentRequestAsync(domainMessage);
                return updateToCancelResult;
            default:
                return Result.Ok();
        }
    }

    private async Task<ProcessCompleteMessagesCommandResponse> HandleBatchDeleteMessages(List<Message> messages, CancellationToken cancellationToken)
    {
        var deleteMessageResponse =
            await executionQueueService.DeleteMessagesAsync(messages, cancellationToken);

        if (deleteMessageResponse.Failed.Count > 0)
        {
            foreach (var failedMessage in deleteMessageResponse.Failed)
            {
                logger.LogError("Message failed to delete from the payment execution queue. MessageId: {MessageId}, ErrorCode:{Code}, Message: {Message}. SenderFault: {SenderFault}",
                    failedMessage.Id, failedMessage.Code, failedMessage.Message, failedMessage.SenderFault);
            }

            return new ProcessCompleteMessagesCommandResponse() { IsProcessingError = true };
        }

        return new ProcessCompleteMessagesCommandResponse() { IsProcessingError = false, SuccessfullyProcessedMessages = deleteMessageResponse.Successful.Count };
    }
}
