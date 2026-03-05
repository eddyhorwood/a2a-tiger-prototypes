using FluentAssertions;
using PaymentExecution.StripeExecutionClient.Options;
using PaymentExecution.StripeExecutionClient.Resilience;
using Polly;
using Polly.Retry;
using Polly.Testing;
namespace PaymentExecution.StripeExecutionClient.UnitTests;

public class StripeExecutionResiliencePipelineTests
{
    [Fact]
    public void GivenConfigurationValues_WhenGetStripeExecutionPipeline_ThenPipelineReturnedWithExpectedConfiguration()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder();
        var stripeExecutionRetryOptions = new StripeExecutionRetryOptions()
        {
            MaxRetryAttempts = 3,
            DelaySeconds = 5,
            UseJitter = true
        };

        // Act
        var pipeline = StripeExecutionResiliencePipeline.GetStripeExecutionPipeline(builder, stripeExecutionRetryOptions);

        // Assert
        var descriptor = pipeline.GetPipelineDescriptor();
        descriptor.Strategies.Count.Should().Be(1);
        var retryOptions = Assert.IsType<RetryStrategyOptions>(descriptor.Strategies[0].Options);
        retryOptions.MaxRetryAttempts.Should().Be(stripeExecutionRetryOptions.MaxRetryAttempts);
        retryOptions.UseJitter.Should().Be(stripeExecutionRetryOptions.UseJitter);
        retryOptions.Delay.Should().Be(TimeSpan.FromSeconds(stripeExecutionRetryOptions.DelaySeconds));
        retryOptions.BackoffType.Should().Be(DelayBackoffType.Exponential);
        retryOptions.ShouldHandle.Should().NotBeNull();
    }
}
