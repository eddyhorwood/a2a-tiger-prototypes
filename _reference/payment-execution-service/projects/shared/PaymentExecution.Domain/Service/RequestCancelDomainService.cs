using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository;
using PaymentExecution.SqsIntegrationClient.Service;

namespace PaymentExecution.Domain.Service;

public interface IRequestCancelDomainService
{
    Task<Result<CancellationRequest>> HandleGetCancellationRequestAsync(Guid paymentRequestId);
    Task<Result> HandleRequestCancellationAsync(
        CancellationRequest cancellationRequest,
        string cancellationReason,
        Guid xeroTenantId,
        Guid xeroCorrelationId);
}

public class RequestCancelDomainService(
    ICancellationValidationService cancellationValidationService,
    IPaymentTransactionRepository repository,
    IMapper mapper,
    ICancelExecutionQueueService cancelExecutionQueueService,
    ILogger<RequestCancelDomainService> logger) : IRequestCancelDomainService
{

    public async Task<Result<CancellationRequest>> HandleGetCancellationRequestAsync(
        Guid paymentRequestId)
    {
        var paymentTransactionDtoResult = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);

        if (paymentTransactionDtoResult.IsFailed)
        {
            return paymentTransactionDtoResult.ToResult();
        }

        if (paymentTransactionDtoResult.Value == null)
        {
            logger.LogInformation("No payment transaction found for payment request id {PaymentRequestId}", paymentRequestId);
            var error = new PaymentExecutionError(
                "Payment transaction not found", ErrorType.PaymentTransactionNotFound, ErrorConstants.ErrorCode.GenericExecutionError);
            return Result.Fail(error);
        }

        var cancellationRequest = mapper.Map<CancellationRequest>(paymentTransactionDtoResult.Value);

        return Result.Ok(cancellationRequest);
    }

    public async Task<Result> HandleRequestCancellationAsync(
        CancellationRequest cancellationRequest,
        string cancellationReason,
        Guid xeroTenantId,
        Guid xeroCorrelationId)
    {
        if (cancellationValidationService.IsPaymentTransactionCancelled(cancellationRequest.Status))
        {
            logger.LogInformation("Payment request with id {PaymentRequestId} is already cancelled. Returning successful result", cancellationRequest.PaymentRequestId);
            return Result.Ok();
        }

        var isCancellable = cancellationValidationService.IsPaymentTransactionCancellable(cancellationRequest);

        if (!isCancellable)
        {
            var error = new PaymentExecutionError("Payment Request is not cancellable",
                ErrorType.PaymentTransactionNotCancellable, ErrorConstants.ErrorCode.ExecutionCancellationError);
            return Result.Fail(error);
        }

        var cancellationQueuePayload = new PaymentCancellationRequest()
        {
            PaymentRequestId = cancellationRequest.PaymentRequestId,
            ProviderType = cancellationRequest.ProviderType.ToString(),
            CancellationReason = cancellationReason
        };

        var sendMessageResult = await cancelExecutionQueueService.SendMessageAsync(cancellationQueuePayload,
            delaySeconds: 0,
            xeroCorrelationId.ToString(),
            xeroTenantId.ToString());

        return sendMessageResult;
    }
}
