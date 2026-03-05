namespace PaymentExecution.Domain.Models;

public class GetProviderStateRequest
{
    public required string ProviderType { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required Guid CorrelationId { get; set; }
    public required Guid TenantId { get; set; }
}
