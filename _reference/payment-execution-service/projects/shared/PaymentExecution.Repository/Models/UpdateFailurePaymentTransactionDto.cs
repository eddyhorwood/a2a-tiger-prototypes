namespace PaymentExecution.Repository.Models;

public record UpdateFailurePaymentTransactionDto
{
    public required Guid PaymentRequestId { get; set; }
    public required Guid ProviderServiceId { get; set; }
    public required string Status { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public string? PaymentProviderPaymentReferenceId { get; set; }
    public string? FailureDetails { get; set; }
    public DateTime? EventCreatedDateTimeUtc { get; set; }
}
