using PaymentExecution.Common.Extensions;
using PaymentExecution.StripeExecutionClient.Options;
using Polly;
using Polly.Retry;
namespace PaymentExecution.StripeExecutionClient.Resilience;

public static class StripeExecutionResiliencePipeline
{
    public static ResiliencePipeline GetStripeExecutionPipeline(ResiliencePipelineBuilder builder, StripeExecutionRetryOptions stripeExecutionRetryOptions)
    {
        var retryOptions = new RetryStrategyOptions()
        {
            MaxRetryAttempts = stripeExecutionRetryOptions.MaxRetryAttempts,
            Delay = TimeSpan.FromSeconds(stripeExecutionRetryOptions.DelaySeconds),
            UseJitter = stripeExecutionRetryOptions.UseJitter,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().HandleTransientErrors()
        };

        return builder.AddRetry(retryOptions).Build();
    }
}
