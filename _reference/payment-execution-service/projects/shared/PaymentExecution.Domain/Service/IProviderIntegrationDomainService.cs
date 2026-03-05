using FluentResults;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Service;

public interface IProviderIntegrationDomainService
{
    ProviderType ProviderType { get; }
    Task<Result<ProviderState>> GetProviderStateAsync(Guid paymentRequestId, Guid? correlationId = null, Guid? tenantId = null);
    Task<Result> CancelPaymentAsync(CancelPaymentRequest cancellationRequest);
}

