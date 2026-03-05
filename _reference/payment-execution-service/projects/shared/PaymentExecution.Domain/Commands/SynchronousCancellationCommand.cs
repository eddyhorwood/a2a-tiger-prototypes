using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;


namespace PaymentExecution.Domain.Commands;

public class SynchronousCancellationCommand : IRequest<Result>
{
    public required Guid PaymentRequestId { get; set; }
    public required string CancellationReason { get; set; }
    public required string TenantId { get; set; }
    public required string CorrelationId { get; set; }
}

public class SynchronousCancellationCommandHandler(
    ICancelDomainService domainService,
    ICancellationValidationService validationService,
    IMapper mapper,
    ILogger<SynchronousCancellationCommandHandler> logger
) : IRequestHandler<SynchronousCancellationCommand, Result>
{
    public async Task<Result> Handle(SynchronousCancellationCommand request, CancellationToken cancellationToken)
    {
        var paymentTransactionResult = await domainService.HandleGetPaymentTransactionRecordAsync(request.PaymentRequestId);
        if (paymentTransactionResult.IsFailed)
        {
            return paymentTransactionResult.ToResult();
        }

        var cancellationRequest = paymentTransactionResult.Value;
        if (validationService.IsPaymentTransactionCancelled(cancellationRequest.Status))
        {
            logger.LogInformation("Payment transaction {PaymentRequestId} is already cancelled. Returning success",
                cancellationRequest.PaymentRequestId);
            return Result.Ok();
        }

        if (!validationService.IsPaymentTransactionCancellable(cancellationRequest))
        {
            logger.LogInformation("Payment transaction {PaymentRequestId} is not cancellable",
                cancellationRequest.PaymentRequestId);
            var error = new PaymentExecutionError("Payment Request is not cancellable",
                ErrorType.PaymentTransactionNotCancellable, ErrorConstants.ErrorCode.ExecutionCancellationError);
            return Result.Fail(error);
        }

        var cancelPaymentRequest = mapper.Map<CancelPaymentRequest>(request);
        var cancellationRequestResult = await domainService.HandleSyncCancellationAsync(cancellationRequest.ProviderType, cancelPaymentRequest);

        return cancellationRequestResult;
    }
}
