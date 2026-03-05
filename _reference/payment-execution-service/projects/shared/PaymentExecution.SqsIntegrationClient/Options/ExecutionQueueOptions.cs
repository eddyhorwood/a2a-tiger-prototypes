namespace PaymentExecution.SqsIntegrationClient.Options;

public class ExecutionQueueOptions : BaseSqsOptions
{
    public static readonly string Key = "SqsConfigurations:ExecutionQueue";
    public required int MaxNumberOfMessages { get; set; }
    public required int LongPollingTimeoutSeconds { get; set; }
}
