using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Specifications.CancellationSpecification;

public interface ICancellationSpecification
{
    bool IsCancellable(CancellationRequest cancellationRequest);
}
