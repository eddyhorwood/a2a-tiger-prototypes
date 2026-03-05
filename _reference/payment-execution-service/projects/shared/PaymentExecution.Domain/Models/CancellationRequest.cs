namespace PaymentExecution.Domain.Models;

public class CancellationRequest
{
    public required ProviderType ProviderType { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required TransactionStatus Status { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; }
}
