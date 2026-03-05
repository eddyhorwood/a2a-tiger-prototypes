namespace PaymentExecution.Repository.Models;

public record SubmitPaymentTransactionDto
{
    public required Guid PaymentRequestId { get; set; }
    public required string Status { get; set; }
    public required string? ProviderType { get; set; }
};
