using FluentResults;
using Microsoft.Extensions.Logging;

namespace PaymentExecution.Domain.Service;
public interface IProviderIntegrationDomainServiceFactory
{
    Result<IProviderIntegrationDomainService> GetProviderIntegrationDomainService(string providerType);
}

public class ProviderIntegrationDomainServiceFactory(
    IEnumerable<IProviderIntegrationDomainService> paymentProviderDomainServices,
    ILogger<ProviderIntegrationDomainServiceFactory> logger) : IProviderIntegrationDomainServiceFactory
{
    public Result<IProviderIntegrationDomainService> GetProviderIntegrationDomainService(string providerType)
    {
        var providerDomainService = paymentProviderDomainServices.FirstOrDefault(h =>
            h.ProviderType.ToString().Equals(providerType, StringComparison.OrdinalIgnoreCase));

        if (providerDomainService == null)
        {
            logger.LogError("Provider type {ProviderType} not supported", providerType);
            return Result.Fail("ProviderType is not supported");
        }

        return Result.Ok(providerDomainService);
    }
}
