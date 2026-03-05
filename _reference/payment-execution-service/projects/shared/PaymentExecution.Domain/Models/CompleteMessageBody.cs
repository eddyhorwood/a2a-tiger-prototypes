using System.Text.Json.Serialization;
using PaymentExecution.Domain.Converters;

namespace PaymentExecution.Domain.Models;

public class CompleteMessageBody
{
    public decimal? Fee { get; set; }

    public string? FeeCurrency { get; set; }

    [JsonConverter(typeof(NonEmptyGuidConverter))]
    public required Guid PaymentRequestId { get; set; }

    [JsonConverter(typeof(NonEmptyGuidConverter))]
    public required Guid ProviderServiceId { get; set; }

    [JsonConverter(typeof(ValidCompleteFlowStatusConverter))]
    public required string Status { get; set; }

    public string? PaymentProviderPaymentTransactionId { get; set; }

    public string? PaymentProviderPaymentReferenceId { get; set; }

    public string? FailureDetails { get; set; }

    public DateTime? EventCreatedDateTime { get; set; }
    public DateTime? PaymentProviderLastUpdatedAt { get; set; }

    public string? CancellationReason { get; set; }

}
