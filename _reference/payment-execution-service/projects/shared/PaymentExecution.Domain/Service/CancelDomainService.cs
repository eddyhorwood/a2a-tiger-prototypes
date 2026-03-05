using System.Net;
using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Util;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.Service;

public interface ICancelDomainService
{
    public Task<Result<CancellationRequest>> HandleGetPaymentTransactionRecordAsync(Guid paymentRequestId);
    public Task<Result<CancellationRequest>> HandleGetPaymentTransactionRecordAsyncForLambda(Guid paymentRequestId);

    public Task<Result> HandleSyncCancellationAsync(ProviderType providerType, CancelPaymentRequest cancelPaymentRequest);
    public Task<Result> HandleLambdaCancellationAsync(ProviderType providerType, CancelPaymentRequest cancelPaymentRequest);
}

public class CancelDomainService(
    IPaymentTransactionRepository repository,
    IMapper mapper,
    IProviderIntegrationDomainServiceFactory providerIntegrationDomainServiceFactory,
    ILogger<CancelDomainService> logger
) : ICancelDomainService
{
    public async Task<Result<CancellationRequest>> HandleGetPaymentTransactionRecordAsync(Guid paymentRequestId)
    {
        var getPaymentTransactionResult = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);
        if (getPaymentTransactionResult.IsFailed)
        {
            return getPaymentTransactionResult.ToResult();
        }

        return MapPaymentTransactionToCancellationRequest(getPaymentTransactionResult.Value, paymentRequestId);
    }

    public async Task<Result<CancellationRequest>> HandleGetPaymentTransactionRecordAsyncForLambda(Guid paymentRequestId)
    {
        var getPaymentTransactionResult = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);
        if (getPaymentTransactionResult.IsFailed)
        {
            return WrapUnexpectedErrorAsTransient(
                getPaymentTransactionResult.ToResult(),
                "Failed to get payment transaction from database");
        }

        return MapPaymentTransactionToCancellationRequest(getPaymentTransactionResult.Value, paymentRequestId);
    }

    public async Task<Result> HandleSyncCancellationAsync(ProviderType providerType, CancelPaymentRequest cancelPaymentRequest)
    {
        var cancelPaymentResult = await CancelPaymentWithProviderDomainService(providerType, cancelPaymentRequest);
        if (cancelPaymentResult.IsFailed)
        {
            var castedError = cancelPaymentResult.Errors[0] as PaymentExecutionError;
            var statusCode = castedError?.GetHttpStatusCode();

            if (castedError == null)
            {
                return cancelPaymentResult;
            }

            if (statusCode == null)
            {
                logger.LogError("No status code is included in error from provider client");
                return Result.Fail("No status code in error from provider client.");
            }

            if ((statusCode == HttpStatusCode.BadRequest &&
                 string.IsNullOrWhiteSpace(castedError.GetProviderErrorCode())) ||
                statusCode == HttpStatusCode.Unauthorized ||
                statusCode == HttpStatusCode.Forbidden)
            {
                logger.LogError("Authorization error when integrating with the provider specific service. Status code: {StatusCode}", (int)statusCode);
                return Result.Fail("Authorization error when integrating with provider specific service.");
            }

            castedError.SetErrorCode(ErrorConstants.ErrorCode.ExecutionCancellationError);
            var isTransientError = (int)statusCode >= 500 || (int)statusCode == 429;
            var errorType = isTransientError
                ? ErrorType.DependencyTransientError
                : ErrorType.PaymentTransactionNotCancellable;

            castedError.SetErrorType(errorType);
            return Result.Fail(castedError);
        }

        return Result.Ok();
    }

    public async Task<Result> HandleLambdaCancellationAsync(ProviderType providerType, CancelPaymentRequest cancelPaymentRequest)
    {
        var cancelPaymentResult = await CancelPaymentWithProviderDomainService(providerType, cancelPaymentRequest);
        if (cancelPaymentResult.IsFailed)
        {
            return ErrorMapper.MapToCancelPaymentErrorForLambda(cancelPaymentResult);
        }

        return Result.Ok();
    }

    private async Task<Result> CancelPaymentWithProviderDomainService(ProviderType providerType, CancelPaymentRequest cancelPaymentRequest)
    {
        var providerIntegrationDomainServiceResult = providerIntegrationDomainServiceFactory.GetProviderIntegrationDomainService(
            providerType.ToString());
        if (providerIntegrationDomainServiceResult.IsFailed)
        {
            return providerIntegrationDomainServiceResult.ToResult();
        }

        var providerIntegrationDomainService = providerIntegrationDomainServiceResult.Value;
        var cancellationResult = await providerIntegrationDomainService.CancelPaymentAsync(cancelPaymentRequest);

        return cancellationResult;
    }

    private static Result<CancellationRequest> WrapUnexpectedErrorAsTransient(Result<CancellationRequest> result, string context)
    {
        var error = result.Errors.FirstOrDefault();

        // If PaymentExecutionError, return as-is (preserves original ErrorType classification)
        if (error is PaymentExecutionError)
        {
            return result;
        }

        // Unexpected error (DB exception, etc.) → wrap as transient for retry
        return Result.Fail(new PaymentExecutionError(
            $"{context}: {error?.Message ?? "Unknown error"}",
            ErrorType.DependencyTransientError,
            ErrorConstants.ErrorCode.ExecutionCancellationError));
    }

    private Result<CancellationRequest> MapPaymentTransactionToCancellationRequest(
        PaymentTransactionDto? paymentTransactionDto,
        Guid paymentRequestId)
    {
        if (paymentTransactionDto == null)
        {
            logger.LogInformation("No payment transaction found for payment request id {PaymentRequestId}", paymentRequestId);
            var error = new PaymentExecutionError(
                "Payment transaction not found",
                ErrorType.PaymentTransactionNotFound,
                ErrorConstants.ErrorCode.ExecutionCancellationError);
            return Result.Fail(error);
        }

        return mapper.Map<CancellationRequest>(paymentTransactionDto);
    }
}
