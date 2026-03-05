namespace PaymentExecution.Domain.Models;

public class PaymentCancellationRequest
{
    public required Guid PaymentRequestId { get; set; }
    public required string ProviderType { get; set; }
    public required string CancellationReason { get; set; }
}
