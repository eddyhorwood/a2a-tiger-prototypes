using System.Net;
using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.Service;

public interface IProcessCompleteMessageDomainService
{
    Result EvaluateIfRecordIsInErrorState(PaymentTransactionDto paymentTransactionDto);

    bool ShouldEventBeIgnored(PaymentTransactionDto paymentTransactionDto, CompleteMessage messageBeingProcessed);

    Task<Result> HandleUpdateDbAsync(CompleteMessage messageBeingProcessed, StripeValidCompleteStatus parsedStatus);

    Task<Result> HandleExecutionSucceedPaymentRequestAsync(CompleteMessage messageBeingProcessed);

    Task<Result> HandleFailPaymentRequestAsync(CompleteMessage messageBeingProcessed);

    Task<Result> HandleCancelPaymentRequestAsync(CompleteMessage messageBeingProcessed);
}

public class ProcessCompleteMessageDomainService(
    IPaymentTransactionRepository paymentTransactionRepository,
    IMapper mapper,
    IPaymentRequestClient paymentRequestClient,
    ILogger<ProcessCompleteMessageDomainService> logger) : IProcessCompleteMessageDomainService
{
    public Result EvaluateIfRecordIsInErrorState(PaymentTransactionDto paymentTransactionDto)
    {
        var paymentProviderTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        var providerServiceId = paymentTransactionDto.ProviderServiceId;
        if (paymentProviderTransactionId == null || providerServiceId == null)
        {
            logger.LogError("Database record does not contain PaymentProviderPaymentTransactionId or ProviderServiceId. PaymentProviderPaymentTransactionId: {PaymentTransactionId}, ProviderServiceId: {ProviderServiceId}," +
                            "PaymentRequestId: {PaymentRequestId}",
                paymentProviderTransactionId, providerServiceId, paymentTransactionDto.PaymentRequestId);
            return Result.Fail("PaymentProviderPaymentTransactionId and ProviderServiceId must not be null for complete flow processing");
        }

        return Result.Ok();
    }

    public bool ShouldEventBeIgnored(PaymentTransactionDto paymentTransactionDto, CompleteMessage messageBeingProcessed)
    {
        //Ignore if event in DB is already terminal and incoming message is of a different state (i.e not a redrive)
        var isDbStatusInTerminalState = Enum.TryParse<TerminalStatus>(paymentTransactionDto.Status, out _);
        var dbStatusAndIncomingMessageStatusDiffer = !string.Equals(paymentTransactionDto.Status, messageBeingProcessed.Status, StringComparison.CurrentCultureIgnoreCase);

        if (isDbStatusInTerminalState && dbStatusAndIncomingMessageStatusDiffer)
        {
            logger.LogInformation("Database record is of terminal state {DbRecordStatus} where incoming status is of different state {IncomingMessageState}. No further updates should occur for {PaymentRequestId}",
                paymentTransactionDto.Status, messageBeingProcessed.Status, paymentTransactionDto.PaymentRequestId);
            return true;
        }

        //Ignore if incoming event is less recent than event in DB
        var incomingEventIsStale =
            Nullable.Compare(paymentTransactionDto.EventCreatedDateTimeUtc, messageBeingProcessed.EventCreatedDateTime) > 0;
        if (incomingEventIsStale)
        {
            logger.LogInformation("Incoming event is stale. {PaymentRequestId}", messageBeingProcessed.PaymentRequestId);
            return true;
        }

        return false;
    }

    public async Task<Result> HandleUpdateDbAsync(CompleteMessage messageBeingProcessed, StripeValidCompleteStatus parsedStatus)
    {
        switch (parsedStatus)
        {
            case StripeValidCompleteStatus.Succeeded:
                {
                    var updateSuccessStatusDto = mapper.Map<UpdateSuccessPaymentTransactionDto>(messageBeingProcessed);
                    var updateResult = await paymentTransactionRepository.UpdateSuccessPaymentTransactionData(updateSuccessStatusDto);
                    return updateResult;
                }
            case StripeValidCompleteStatus.Failed:
                {
                    var updateFailureStatusDto = mapper.Map<UpdateFailurePaymentTransactionDto>(messageBeingProcessed);
                    var updateResult = await paymentTransactionRepository.UpdateFailurePaymentTransactionData(updateFailureStatusDto);
                    return updateResult;
                }
            case StripeValidCompleteStatus.Cancelled:
                {
                    var updateCancelledStatusDto = mapper.Map<UpdateCancelledPaymentTransactionDto>(messageBeingProcessed);
                    var updateResult = await paymentTransactionRepository.UpdateCancelledPaymentTransactionData(updateCancelledStatusDto);
                    return updateResult;
                }
            default:
                logger.LogError("Received unexpected status {Status} when handling Payment transaction update. PaymentRequestId: {PaymentRequestId}",
                    parsedStatus, messageBeingProcessed.PaymentRequestId);
                return Result.Fail("Status was not of expected type");
        }
    }

    public async Task<Result> HandleExecutionSucceedPaymentRequestAsync(CompleteMessage messageBeingProcessed)
    {
        var successCommandRequest = mapper.Map<SuccessPaymentRequest>(messageBeingProcessed);
        var executionSucceedResult = await paymentRequestClient.ExecutionSucceedPaymentRequest(messageBeingProcessed.PaymentRequestId, successCommandRequest, messageBeingProcessed.XeroCorrelationId, messageBeingProcessed.XeroTenantId);

        if (executionSucceedResult.IsFailed)
        {
            var castedError = executionSucceedResult.Errors.FirstOrDefault() as PaymentExecutionError;
            var executionSucceedStatusCode = castedError?.GetHttpStatusCode() ?? throw new InvalidOperationException("Error is not of type PaymentExecutionError with HttpStatusCode");

            if (executionSucceedStatusCode != HttpStatusCode.BadRequest)
            {
                return executionSucceedResult;
            }

            var handleBadRequestResult = await HandleExecutionSuccessBadRequestAsync(messageBeingProcessed);
            return handleBadRequestResult;
        }

        return Result.Ok();
    }

    public async Task<Result> HandleFailPaymentRequestAsync(CompleteMessage messageBeingProcessed)
    {
        var failureCommandRequest = mapper.Map<FailurePaymentRequest>(messageBeingProcessed);
        var failPaymentRequestResult = await paymentRequestClient.FailPaymentRequest(messageBeingProcessed.PaymentRequestId, failureCommandRequest, messageBeingProcessed.XeroCorrelationId, messageBeingProcessed.XeroTenantId);

        return failPaymentRequestResult.IsSuccess ? Result.Ok() : failPaymentRequestResult;
    }

    public async Task<Result> HandleCancelPaymentRequestAsync(CompleteMessage messageBeingProcessed)
    {
        var cancelCommandRequest = mapper.Map<PaymentExecution.PaymentRequestClient.Models.Requests.CancelPaymentRequest>(messageBeingProcessed);
        var cancelPaymentRequestResult = await paymentRequestClient.CancelPaymentRequest(messageBeingProcessed.PaymentRequestId, cancelCommandRequest, messageBeingProcessed.XeroCorrelationId, messageBeingProcessed.XeroTenantId);

        return cancelPaymentRequestResult.IsSuccess ? Result.Ok() : cancelPaymentRequestResult;
    }

    private async Task<Result> HandleExecutionSuccessBadRequestAsync(CompleteMessage messageBeingProcessed)
    {
        var getPaymentRequestResult = await paymentRequestClient.GetPaymentRequestByPaymentRequestId(messageBeingProcessed.PaymentRequestId, messageBeingProcessed.XeroCorrelationId);
        if (getPaymentRequestResult.IsFailed)
        {
            return getPaymentRequestResult.ToResult();
        }

        var paymentRequest = mapper.Map<PaymentRequest>(getPaymentRequestResult.Value);
        if (paymentRequest.Status != RequestStatus.success)
        {
            logger.LogError("Payment request has an unexpected status of {Status} when evaluating executionsuccess Bad Request response. PaymentRequestId: {PaymentRequestId}",
                paymentRequest.Status, paymentRequest.PaymentRequestId);
            return Result.Fail("Payment request is in unexpected state");
        }

        logger.LogInformation("Payment request is already in success state for PaymentRequestId: {PaymentRequestId}. Successfully processing message",
            messageBeingProcessed.PaymentRequestId);
        return Result.Ok();
    }
}
