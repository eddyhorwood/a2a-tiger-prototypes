using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Specifications.CancellationSpecification;

namespace PaymentExecution.Domain.Service;

public interface ICancellationValidationService
{
    bool IsPaymentTransactionCancelled(TransactionStatus status);
    bool IsPaymentTransactionCancellable(CancellationRequest cancellationRequest);
    bool IsPaymentAutoCancellable(PaymentProviderStatus providerStatus);
}

public class CancellationValidationService(IEnumerable<ICancellationSpecification> cancellationSpecifications) : ICancellationValidationService
{
    public bool IsPaymentTransactionCancelled(TransactionStatus status)
    {
        return status.Equals(TransactionStatus.Cancelled);
    }

    public bool IsPaymentTransactionCancellable(CancellationRequest cancellationRequest)
    {
        return cancellationSpecifications.All(spec => spec.IsCancellable(cancellationRequest));
    }

    public bool IsPaymentAutoCancellable(PaymentProviderStatus providerStatus)
    {
        return providerStatus is (PaymentProviderStatus.Submitted);
    }
}
