namespace PaymentExecution.Repository.Models;

public record InsertPaymentTransactionDto
{
    public required Guid PaymentRequestId { get; set; }
    public required string Status { get; set; }
    public required string? ProviderType { get; set; }
    public required Guid OrganisationId { get; set; }
};
