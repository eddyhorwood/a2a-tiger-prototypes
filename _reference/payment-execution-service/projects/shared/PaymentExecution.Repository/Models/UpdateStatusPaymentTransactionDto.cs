namespace PaymentExecution.Repository.Models;

public record UpdateStatusPaymentTransactionDto
{
    public required Guid PaymentRequestId { get; set; }
    public required Guid ProviderServiceId { get; set; }
    public required string Status { get; set; }
}
