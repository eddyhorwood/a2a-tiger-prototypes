using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Specifications.CancellationSpecification;

public class ProviderPaymentTransactionIdSpecification(ILogger<ProviderPaymentTransactionIdSpecification> logger) : ICancellationSpecification
{
    public bool IsCancellable(CancellationRequest cancellationRequest)
    {
        if (string.IsNullOrWhiteSpace(cancellationRequest.PaymentProviderPaymentTransactionId))
        {
            logger.LogInformation("Payment Transaction is not cancellable. PaymentProviderPaymentTransactionId is null or empty for PaymentRequestId: {PaymentRequestId}",
                cancellationRequest.PaymentRequestId);
            return false;
        }

        return true;
    }
}
