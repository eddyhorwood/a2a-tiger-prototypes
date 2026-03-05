namespace PaymentExecution.PaymentRequestClient.Models.Requests;

public class SuccessPaymentRequest
{
    public required DateTime PaymentCompletionDateTime { get; set; }
    public required decimal Fee { get; set; }
    public required string FeeCurrency { get; set; }

    /// <summary>
    /// IntentId for stripe
    /// </summary>
    public required string PaymentProviderPaymentTransactionId { get; set; }

    /// <summary>
    /// ChargeId for stripe
    /// </summary>
    public required string PaymentProviderPaymentReferenceId { get; set; }
    public required DateTime PaymentProviderLastUpdatedAt { get; set; }
}
