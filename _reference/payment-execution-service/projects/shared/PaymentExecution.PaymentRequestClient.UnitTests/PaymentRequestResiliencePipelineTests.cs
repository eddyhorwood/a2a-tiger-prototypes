using FluentAssertions;
using PaymentExecution.PaymentRequestClient.Resilience;
using Polly;
using Polly.Retry;
using Polly.Testing;
namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestResiliencePipelineTests
{
    [Fact]
    public void GivenConfigurationValues_WhenGetPaymentRequestPipeline_ThenPipelineReturnedWithExpectedConfiguration()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder();
        var paymentRequestRetryOptions = new PaymentRequestRetryOptions()
        {
            MaxRetryAttempts = 3,
            DelaySeconds = 5,
            UseJitter = true
        };

        // Act
        var pipeline = PaymentRequestResiliencePipeline.GetPaymentRequestPipeline(builder, paymentRequestRetryOptions);

        // Assert
        var descriptor = pipeline.GetPipelineDescriptor();
        descriptor.Strategies.Count.Should().Be(1);
        var retryOptions = Assert.IsType<RetryStrategyOptions>(descriptor.Strategies[0].Options);
        retryOptions.MaxRetryAttempts.Should().Be(paymentRequestRetryOptions.MaxRetryAttempts);
        retryOptions.UseJitter.Should().Be(paymentRequestRetryOptions.UseJitter);
        retryOptions.Delay.Should().Be(TimeSpan.FromSeconds(paymentRequestRetryOptions.DelaySeconds));
        retryOptions.BackoffType.Should().Be(DelayBackoffType.Exponential);
        retryOptions.ShouldHandle.Should().NotBeNull();
    }
}
