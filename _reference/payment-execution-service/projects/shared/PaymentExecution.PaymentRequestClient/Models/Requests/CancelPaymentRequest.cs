namespace PaymentExecution.PaymentRequestClient.Models.Requests;

public class CancelPaymentRequest
{
    public required string PaymentProviderPaymentTransactionId { get; set; } //intentId for stripe
    public DateTime PaymentProviderLastUpdatedAt { get; set; }
    public required string CancellationReason { get; set; }
}
