namespace PaymentExecution.StripeExecutionClient.Options;
public class StripeExecutionServiceOptions
{
    public required string BaseUrl { get; set; }
    public required string SubmitEndpoint { get; set; }
    public required string CancelEndpoint { get; set; }
    public required string GetPaymentIntentEndpoint { get; set; }
}
