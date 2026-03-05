namespace PaymentExecution.PaymentRequestClient;
public class PaymentRequestServiceOptions
{
    public required string BaseUrl { get; set; }
    public required string SubmitPaymentRequestEndpoint { get; set; }
    public required string ExecutionSuccessPaymentRequestEndpoint { get; set; }
    public required string FailurePaymentRequestEndpoint { get; set; }
    public required string GetPaymentRequestByIdEndpoint { get; set; }

    public required string CancelExecutionInProgressPaymentRequestEndpoint { get; set; }
}
