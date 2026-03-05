using System.Text.Json.Serialization;
using PaymentExecution.Domain.Converters;

namespace PaymentExecutionLambda.CancelLambda.Models;

/// <summary>
/// Represents a request to cancel a payment, including the payment request ID, provider type, and cancellation reason.
/// </summary>
public class CancelPaymentRequest
{
    /// <summary>
    /// The unique identifier of the payment request to be cancelled.
    /// </summary>
    [JsonPropertyName("paymentRequestId")]
    [JsonConverter(typeof(NonEmptyGuidConverter))]
    public required Guid PaymentRequestId { get; set; }

    /// <summary>
    /// The type of payment provider. Expected values: e.g., "Stripe", etc.
    /// </summary>
    [JsonPropertyName("providerType")]
    public required string ProviderType { get; set; }

    /// <summary>
    /// The reason for cancelling the payment.
    /// </summary>
    [JsonPropertyName("cancellationReason")]
    public required string CancellationReason { get; set; }
}
