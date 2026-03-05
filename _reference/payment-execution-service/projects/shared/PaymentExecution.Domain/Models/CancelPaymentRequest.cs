namespace PaymentExecution.Domain.Models;

public class CancelPaymentRequest
{
    public required Guid TenantId { get; set; }
    public required Guid CorrelationId { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required string CancellationReason { get; set; }
}

