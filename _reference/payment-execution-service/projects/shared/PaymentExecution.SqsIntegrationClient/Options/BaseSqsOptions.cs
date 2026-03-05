namespace PaymentExecution.SqsIntegrationClient.Options;

public abstract class BaseSqsOptions
{
    public required string QueueUrl { get; init; }
}
