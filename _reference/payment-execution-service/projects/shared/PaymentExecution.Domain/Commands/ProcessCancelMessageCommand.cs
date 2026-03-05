using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.FeatureFlagClient;

namespace PaymentExecution.Domain.Commands;

public class ProcessCancelMessageCommand : IRequest<Result>
{
    public Guid TenantId { get; set; }
    public Guid CorrelationId { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required string ProviderType { get; set; }
    public required string CancellationReason { get; set; }
}

public class ProcessCancelMessageCommandHandler(
    ILogger<ProcessCancelMessageCommandHandler> logger,
    IGetProviderStateDomainService getProviderStateDomainService,
    ICancelDomainService cancelDomainService,
    ICancellationValidationService validationService,
    IFeatureFlagClient featureFlagClient,
    IMapper mapper)
    : IRequestHandler<ProcessCancelMessageCommand, Result>
{
    public async Task<Result> Handle(
        ProcessCancelMessageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing cancellation request for PaymentRequestId {PaymentRequestId}, Provider: {ProviderType}, CancellationReason: {CancellationReason}",
            request.PaymentRequestId,
            request.ProviderType,
            request.CancellationReason);

        // Fail fast: validate provider type before DB call
        if (!IsValidProviderType(request.ProviderType, request.PaymentRequestId))
        {
            return Result.Fail(new PaymentExecutionError("Invalid provider type",
                ErrorType.ValidationError,
                ErrorConstants.ErrorCode.ExecutionCancellationError));
        }

        var cancellationRequestResult = await cancelDomainService.HandleGetPaymentTransactionRecordAsyncForLambda(request.PaymentRequestId);
        if (cancellationRequestResult.IsFailed)
        {
            return cancellationRequestResult.ToResult();
        }

        var cancellationRequest = cancellationRequestResult.Value;
        // Check if already cancelled (idempotent)
        if (validationService.IsPaymentTransactionCancelled(cancellationRequest.Status))
        {
            logger.LogInformation("Payment transaction {PaymentRequestId} is already cancelled",
                request.PaymentRequestId);
            return Result.Ok();
        }

        // Validate cancellation eligibility
        if (!validationService.IsPaymentTransactionCancellable(cancellationRequest))
        {
            return Result.Fail(new PaymentExecutionError(
                $"Payment transaction is not cancellable. Status: {cancellationRequest.Status}",
                ErrorType.ValidationError,
                ErrorConstants.ErrorCode.ExecutionCancellationError));
        }

        logger.LogInformation(
            "Payment transaction {PaymentRequestId} is cancellable, verifying provider status",
            request.PaymentRequestId);

        // Ensure request is not in Action-Required state before proceeding with cancellation
        var getProviderStateRequest = mapper.Map<GetProviderStateRequest>(request);
        getProviderStateRequest.ProviderType = cancellationRequest.ProviderType.ToString();
        var getProviderStateResult = await getProviderStateDomainService.HandleGetProviderStateForLambdaAsync(getProviderStateRequest);

        if (getProviderStateResult.IsFailed)
        {
            return getProviderStateResult.ToResult();
        }

        var providerState = getProviderStateResult.Value;

        if (!validationService.IsPaymentAutoCancellable(providerState.PaymentProviderStatus))
        {
            return Result.Fail(new PaymentExecutionError(
                $"Payment is not auto-cancellable. Provider status: {providerState.PaymentProviderStatus}",
                ErrorType.ValidationError,
                ErrorConstants.ErrorCode.ExecutionCancellationError));
        }

        logger.LogInformation(
            "Payment transaction {PaymentRequestId} is eligible for auto-cancellation, proceeding with provider cancellation",
            request.PaymentRequestId);

        var enableProviderCancellationFeatureFlag =
            featureFlagClient.GetFeatureFlag(ExecutionConstants.FeatureFlags.EnableProviderCancellation);

        if (enableProviderCancellationFeatureFlag.Value)
        {
            var cancelRequest = mapper.Map<CancelPaymentRequest>(request);

            var cancelResult = await cancelDomainService.HandleLambdaCancellationAsync(cancellationRequest.ProviderType, cancelRequest);

            if (cancelResult.IsFailed)
            {
                return cancelResult;
            }
        }

        return Result.Ok();
    }

    private bool IsValidProviderType(string providerType, Guid paymentRequestId)
    {
        var isValid = Enum.TryParse<ProviderType>(providerType, ignoreCase: true, out _);

        if (!isValid)
        {
            logger.LogError(
                "Invalid provider type '{ProviderType}' for PaymentRequestId {PaymentRequestId}",
                providerType,
                paymentRequestId);
        }

        return isValid;
    }
}
