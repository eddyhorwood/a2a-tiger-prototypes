namespace PaymentExecution.StripeExecutionClient.Options;

public class StripeExecutionRetryOptions
{
    public static readonly string Key = "ResilienceConfigurations:StripeExecutionRetryOptions";
    public required int MaxRetryAttempts { get; set; }
    public required int DelaySeconds { get; set; }
    public required bool UseJitter { get; set; }
}
