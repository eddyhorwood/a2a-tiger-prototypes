using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Specifications.CancellationSpecification;

public class EligibleStatusSpecification(ILogger<EligibleStatusSpecification> logger) : ICancellationSpecification
{
    private readonly HashSet<TransactionStatus> _eligibleStatuses = new HashSet<TransactionStatus>()
    {
        TransactionStatus.Submitted
    };

    public bool IsCancellable(CancellationRequest cancellationRequest)
    {
        var isCancellableStatus = _eligibleStatuses.Contains(cancellationRequest.Status);

        if (!isCancellableStatus)
        {
            logger.LogInformation("Payment Transaction is not cancellable. Status {Status} is not cancellable. PaymentRequestId: {PaymentRequestId}",
                cancellationRequest.Status, cancellationRequest.PaymentRequestId);
            return false;
        }

        return true;
    }
}
