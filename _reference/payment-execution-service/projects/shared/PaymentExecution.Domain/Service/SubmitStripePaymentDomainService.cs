using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Util;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.SqsIntegrationClient.Service;
using PaymentExecution.StripeExecutionClient.Contracts;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.Service;

public interface ISubmitStripePaymentDomainService
{
    Task<Result<PaymentRequest>> SubmitToPaymentRequestAsync(
        Guid paymentRequestId);
    Task<Result<Guid>> CreatePaymentTransactionWithCompensationActionAsync(Guid paymentRequestId, Guid organisationId);

    Task<Result<SubmittedPayment>> SubmitRequestToStripeExecutionWithCompensationActionAsync(
        PaymentRequest paymentRequest,
        List<string>? paymentMethodsMadeAvailable, string? paymentMethodId);

    Task TryToUpdatePaymentTransactionWithProviderDetailsAsync(string paymentProviderPaymentTransactionId, Guid paymentTransactionId, Guid providerServiceId, Guid paymentRequestId);

    Task TryToSendMessageToCancelExecutionQueueAsync(Guid paymentRequestId, int ttlInSeconds, string xeroCorrelationId, string xeroTenantId);
}

public class SubmitStripePaymentDomainService(
    IPaymentRequestClient paymentRequestClient,
    IPaymentTransactionRepository paymentTransactionRepository,
    IStripeExecutionClient stripeExecutionClient,
    ILogger<SubmitStripePaymentDomainService> logger,
    IMapper mapper,
    ICancelExecutionQueueService cancelExecutionQueueService) : ISubmitStripePaymentDomainService
{

    public async Task<Result<PaymentRequest>> SubmitToPaymentRequestAsync(
        Guid paymentRequestId)
    {
        var paymentRequestResult = await paymentRequestClient.SubmitPaymentRequest(paymentRequestId);
        if (paymentRequestResult.IsFailed)
        {
            var transformedError = ErrorMapper.MapToBadPaymentRequestError(paymentRequestResult.ToResult());
            return Result.Fail(transformedError);
        }

        var domainPaymentRequest = mapper.Map<PaymentRequest>(paymentRequestResult.Value);
        return domainPaymentRequest;
    }

    public async Task<Result<Guid>> CreatePaymentTransactionWithCompensationActionAsync(Guid paymentRequestId, Guid organisationId)
    {
        var insertPaymentTransactionDto = new InsertPaymentTransactionDto()
        {
            PaymentRequestId = paymentRequestId,
            Status = nameof(TransactionStatus.Submitted),
            ProviderType = nameof(ProviderType.Stripe),
            OrganisationId = organisationId
        };

        var transactionIdResult =
            await paymentTransactionRepository.InsertPaymentTransactionIfNotExist(insertPaymentTransactionDto);
        if (transactionIdResult.IsFailed)
        {
            await CompensateInsertPaymentTransactionFailureAsync(paymentRequestId, transactionIdResult.ToResult());

            var transformedError = ErrorMapper.MapToPaymentFailedError(transactionIdResult.ToResult());
            return Result.Fail(transformedError);
        }

        return transactionIdResult;
    }

    public async Task<Result<SubmittedPayment>> SubmitRequestToStripeExecutionWithCompensationActionAsync(
        PaymentRequest paymentRequest,
        List<string>? paymentMethodsMadeAvailable, string? paymentMethodId)
    {
        var stripeExecutionResult = await stripeExecutionClient.SubmitPaymentAsync(new StripeExeSubmitPaymentRequestDto()
        {
            PaymentRequest = mapper.Map<StripeExePaymentRequestDto>(paymentRequest),
            PaymentMethodsMadeAvailable = paymentMethodsMadeAvailable,
            PaymentMethodId = paymentMethodId
        });


        if (stripeExecutionResult.IsFailed)
        {
            await CompensateStripeExecutionFailureAsync(paymentRequest.PaymentRequestId, stripeExecutionResult.ToResult());

            var transformedError = ErrorMapper.MapToPaymentFailedError(stripeExecutionResult.ToResult());
            return Result.Fail(transformedError);
        }

        var domainSubmitResponse = mapper.Map<SubmittedPayment>(stripeExecutionResult.Value);
        return domainSubmitResponse;
    }

    private async Task CompensateInsertPaymentTransactionFailureAsync(Guid paymentRequestId, Result failureResult)
    {
        //update the payment request status to Failed
        //Currently this step will throw exception if failed
        var failureDetails = failureResult.GetFirstErrorMessage();

        await paymentRequestClient.FailPaymentRequest(
            paymentRequestId,
            new FailurePaymentRequest() { FailureDetails = failureDetails });
    }

    private async Task CompensateStripeExecutionFailureAsync(Guid paymentRequestId, Result failureResult)
    {
        // If failed to submit to provider execution service, we need to
        // 1) update the payment transaction status to Failed
        var failureDetails = failureResult.GetFirstErrorMessage();
        if (failureResult.Errors.FirstOrDefault() is PaymentExecutionError castedError)
        {
            var providerErrorCode = castedError.GetProviderErrorCode();
            if (!string.IsNullOrWhiteSpace(castedError.GetProviderErrorCode()))
            {
                failureDetails = string.Concat(providerErrorCode, "-", failureDetails);
            }
        }

        var setPaymentTransactionFailedResult =
            await paymentTransactionRepository.SetPaymentTransactionFailed(
                paymentRequestId, failureDetails, nameof(TransactionStatus.Failed));
        if (setPaymentTransactionFailedResult.IsFailed)
        {
            //todo: not a good idea to throw an exception here, but it works to block the payment submission
            throw new Exception(setPaymentTransactionFailedResult.GetFirstErrorMessage());
        }

        // 2) update the payment request status to Failed
        // Currently this step will throw exception if failed
        await paymentRequestClient.FailPaymentRequest(
            paymentRequestId,
            new FailurePaymentRequest() { FailureDetails = failureDetails });
    }

    public async Task TryToUpdatePaymentTransactionWithProviderDetailsAsync(string paymentProviderPaymentTransactionId,
        Guid paymentTransactionId, Guid providerServiceId, Guid paymentRequestId)
    {
        var dto = new UpdateForSubmitFlowDto()
        {
            PaymentTransactionId = paymentTransactionId,
            ProviderServiceId = providerServiceId,
            PaymentProviderPaymentTransactionId = paymentProviderPaymentTransactionId
        };
        var result =
            await paymentTransactionRepository.UpdatePaymentTransactionWithProviderDetails(dto);

        //This is a best effort update, so we log the error but DO NOT fail the command and block the payment submission
        if (result.IsFailed)
        {
            //Do not delete this log - used for alerting
            logger.LogError(
                "Failed to update payment transaction with provider details. Message: {Message}. PaymentRequestId: {PaymentRequestId}, PaymentProviderPaymentTransactionId: {PaymentProviderPaymentTransactionId}, " +
                "ProviderServiceId: {ProviderServiceId}",
                result.GetFirstErrorMessage(), paymentRequestId, paymentProviderPaymentTransactionId, providerServiceId);
        }
    }

    public async Task TryToSendMessageToCancelExecutionQueueAsync(
        Guid paymentRequestId,
        int ttlInSeconds,
        string xeroCorrelationId,
        string xeroTenantId)
    {
        var cancelMessage = new PaymentCancellationRequest
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            CancellationReason = nameof(CancellationReason.Abandoned)
        };

        var result = await cancelExecutionQueueService.SendMessageAsync(
            cancelMessage,
            ttlInSeconds,
            xeroCorrelationId,
            xeroTenantId);

        // Log if sending message failed, but don't fail the entire operation
        // This is a best-effort operation similar to updating payment transaction
        if (result.IsFailed)
        {
            logger.LogWarning(
                "Failed to send message to cancel execution queue. Message: {Message}. PaymentRequestId: {PaymentRequestId}",
                result.GetFirstErrorMessage(), paymentRequestId);
        }
    }
}
