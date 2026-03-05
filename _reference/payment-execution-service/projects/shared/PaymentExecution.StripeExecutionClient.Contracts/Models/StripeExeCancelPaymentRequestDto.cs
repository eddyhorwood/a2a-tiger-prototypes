namespace PaymentExecution.StripeExecutionClient.Contracts.Models;

public class StripeExeCancelPaymentRequestDto
{
    public required Guid TenantId { get; set; }
    public required Guid CorrelationId { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required string CancellationReason { get; set; }
}
