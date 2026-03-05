using PaymentExecution.Common.Extensions;
using Polly;
using Polly.Retry;

namespace PaymentExecution.PaymentRequestClient.Resilience;

public static class PaymentRequestResiliencePipeline
{
    public static ResiliencePipeline GetPaymentRequestPipeline(ResiliencePipelineBuilder builder, PaymentRequestRetryOptions paymentRequestRetryOptions)
    {
        var retryOptions = new RetryStrategyOptions()
        {
            MaxRetryAttempts = paymentRequestRetryOptions.MaxRetryAttempts,
            Delay = TimeSpan.FromSeconds(paymentRequestRetryOptions.DelaySeconds),
            UseJitter = paymentRequestRetryOptions.UseJitter,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().HandleTransientErrors()
        };

        return builder.AddRetry(retryOptions).Build();
    }
}
