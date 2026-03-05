namespace PaymentExecution.Repository.Models;

public record PaymentTransactionDto
{
    public Guid? PaymentTransactionId { get; set; }

    public required Guid PaymentRequestId { get; set; }
    public Guid? ProviderServiceId { get; set; }
    public required string Status { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; }
    public string? PaymentProviderPaymentReferenceId { get; set; }
    public string? FailureDetails { get; set; }
    public DateTime? EventCreatedDateTimeUtc { get; set; }
    public required string ProviderType { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public string? CancellationReason { get; set; }
}
