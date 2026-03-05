namespace PaymentExecution.Domain.Models;

public class CompleteMessage
{
    public Guid PaymentRequestId { get; set; }
    public Guid ProviderServiceId { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public required string Status { get; set; }
    public required string XeroCorrelationId { get; set; }
    public required string XeroTenantId { get; set; }
    public required string MessageId { get; set; }
    public required string ReceiptHandle { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; }
    public string? PaymentProviderPaymentReferenceId { get; set; }
    public string? FailureDetails { get; set; }
    public DateTime? EventCreatedDateTime { get; set; }
    public DateTime? PaymentProviderLastUpdatedAt { get; set; }

    public string? CancellationReason { get; set; }
}
