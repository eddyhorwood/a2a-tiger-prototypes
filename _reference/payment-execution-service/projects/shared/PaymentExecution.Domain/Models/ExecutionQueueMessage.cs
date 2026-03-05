using System.Diagnostics.CodeAnalysis;

namespace PaymentExecution.Domain.Models;

[ExcludeFromCodeCoverage]
public record ExecutionQueueMessage
{
    public required Guid PaymentRequestId { get; set; }
    public required Guid ProviderServiceId { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public required string ProviderType { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; }
    public string? PaymentProviderPaymentReferenceId { get; set; }
    public required string Status { get; set; }
    public string? FailureDetails { get; set; }
    public DateTime? EventCreatedDateTime { get; set; }
    public DateTime? PaymentProviderLastUpdatedAt { get; set; }
    public string? CancellationReason { get; set; }
}
