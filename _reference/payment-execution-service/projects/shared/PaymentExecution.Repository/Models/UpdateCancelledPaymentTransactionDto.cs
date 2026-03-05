namespace PaymentExecution.Repository.Models;

public class UpdateCancelledPaymentTransactionDto
{
    public required Guid PaymentRequestId { get; set; }
    public required Guid ProviderServiceId { get; set; }
    public required string Status { get; set; }
    public DateTime? EventCreatedDateTimeUtc { get; set; }
    public string? CancellationReason { get; set; }
}
