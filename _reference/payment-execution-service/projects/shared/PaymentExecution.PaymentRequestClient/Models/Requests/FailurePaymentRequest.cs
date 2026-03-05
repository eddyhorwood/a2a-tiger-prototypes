namespace PaymentExecution.PaymentRequestClient.Models.Requests;

public class FailurePaymentRequest
{
    public DateTime? PaymentCompletionDateTime { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; } //intentId for stripe
    public string? PaymentProviderPaymentReferenceId { get; set; } //chargeid for stripe
    public required string FailureDetails { get; set; }
}
