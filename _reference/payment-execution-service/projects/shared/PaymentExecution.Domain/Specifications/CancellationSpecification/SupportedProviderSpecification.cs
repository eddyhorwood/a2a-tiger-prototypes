using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Specifications.CancellationSpecification;

public class SupportedProviderSpecification(ILogger<SupportedProviderSpecification> logger) : ICancellationSpecification
{
    private readonly HashSet<ProviderType> _supportedProviders =
        new HashSet<ProviderType>()
        {
            ProviderType.Stripe
        };

    public bool IsCancellable(CancellationRequest cancellationRequest)
    {
        var isProviderSupported = _supportedProviders.Contains(cancellationRequest.ProviderType);

        if (!isProviderSupported)
        {
            logger.LogInformation("Payment Transaction is not cancellable. Payment provider is not supported: {ProviderType}, PaymentRequestId {PaymentRequestId}",
                cancellationRequest.ProviderType, cancellationRequest.PaymentRequestId);
            return false;
        }

        return true;
    }
}
