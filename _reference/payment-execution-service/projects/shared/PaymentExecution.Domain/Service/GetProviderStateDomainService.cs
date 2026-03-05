using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Util;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.Service;

public interface IGetProviderStateDomainService
{
    Task<Result<PaymentTransactionDto>> HandleGetPaymentTransactionById(Guid paymentRequestId);
    Task<Result<ProviderState>> HandleGetProviderStateAsync(string providerType, Guid paymentRequestId);
    Task<Result<ProviderState>> HandleGetProviderStateForLambdaAsync(GetProviderStateRequest request);
}

public class GetProviderStateDomainService(
    IPaymentTransactionRepository repository,
    IProviderIntegrationDomainServiceFactory providerIntegrationDomainServiceFactory,
    ILogger<GetProviderStateDomainService> logger) : IGetProviderStateDomainService
{
    public async Task<Result<PaymentTransactionDto>> HandleGetPaymentTransactionById(Guid paymentRequestId)
    {
        var paymentTransactionResult = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);
        if (paymentTransactionResult.IsFailed)
        {
            return paymentTransactionResult.ToResult();
        }

        if (paymentTransactionResult.Value == null)
        {
            logger.LogError("PaymentTransaction not found for PaymentRequestId: {PaymentRequestId}", paymentRequestId);
            var error = new PaymentExecutionError(
                ErrorConstants.ErrorMessage.PaymentTransactionNotFoundError,
                ErrorType.PaymentTransactionNotFound,
                ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
            return Result.Fail(error);
        }

        return Result.Ok(paymentTransactionResult.Value);
    }

    public async Task<Result<ProviderState>> HandleGetProviderStateAsync(string providerType, Guid paymentRequestId)
    {
        var getProviderIntegrationServiceResult = providerIntegrationDomainServiceFactory.GetProviderIntegrationDomainService(providerType);
        if (getProviderIntegrationServiceResult.IsFailed)
        {
            return getProviderIntegrationServiceResult.ToResult();
        }
        var providerIntegrationService = getProviderIntegrationServiceResult.Value;

        var providerStateResult = await providerIntegrationService.GetProviderStateAsync(paymentRequestId);
        if (providerStateResult.IsFailed)
        {
            var mappedResult = ErrorMapper.MapToGetProviderError(providerStateResult.ToResult());
            return mappedResult;
        }

        return providerStateResult;
    }

    public async Task<Result<ProviderState>> HandleGetProviderStateForLambdaAsync(GetProviderStateRequest request)
    {
        var getProviderIntegrationServiceResult = providerIntegrationDomainServiceFactory.GetProviderIntegrationDomainService(request.ProviderType);
        if (getProviderIntegrationServiceResult.IsFailed)
        {
            return Result.Fail(new PaymentExecutionError(
                "Failed to get provider integration service",
                ErrorType.FailedDependency,
                ErrorConstants.ErrorCode.ExecutionGetProviderStateError));
        }
        var providerIntegrationService = getProviderIntegrationServiceResult.Value;

        var providerStateResult = await providerIntegrationService.GetProviderStateAsync(request.PaymentRequestId, request.CorrelationId, request.TenantId);
        if (providerStateResult.IsFailed)
        {
            var mappedResult = ErrorMapper.MapToGetProviderErrorForLambda(providerStateResult.ToResult());
            return mappedResult;
        }

        return providerStateResult;
    }
}
