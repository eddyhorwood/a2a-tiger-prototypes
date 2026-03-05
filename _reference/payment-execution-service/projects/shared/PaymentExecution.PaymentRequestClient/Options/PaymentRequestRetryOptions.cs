namespace PaymentExecution.PaymentRequestClient;

public class PaymentRequestRetryOptions
{
    public static readonly string Key = "ResilienceConfigurations:PaymentRequestRetryOptions";
    public required int MaxRetryAttempts { get; set; }
    public required int DelaySeconds { get; set; }
    public required bool UseJitter { get; set; }
}
