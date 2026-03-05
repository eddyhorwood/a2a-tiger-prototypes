using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecutionService.Controllers.V1;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using Xunit;

namespace PaymentExecutionService.ComponentTests.Middleware;

public class ActionCircuitBreakerTests
{
    private const string Version1StringPrefix = "v1.0:";

    private readonly ComponentTestsFixture _fixture = new PortableComponentTestsFixture();

    private const string CompleteTransactionPipelinePath = Version1StringPrefix + nameof(PaymentsController) + ":" + nameof(PaymentsController.CompletePaymentTransaction);
    private const string GetPaymentTransactionPipelinePath = Version1StringPrefix + nameof(PaymentsController) + ":" + nameof(PaymentsController.GetPaymentTransaction);
    private const string DchDeletePipelinePath = Version1StringPrefix + nameof(DchDeleteController) + ":" + nameof(DchDeleteController.DchDelete);
    private const string SubmitPaymentPipelinePath = Version1StringPrefix + nameof(SubmitStripeController) + ":" + nameof(SubmitStripeController.SubmitPayment);

    private static readonly CircuitBreakerStrategyOptions _expectedDevEnvCircuitOptions = new()
    {
        FailureRatio = 0.1,
        BreakDuration = TimeSpan.FromSeconds(30),
        SamplingDuration = TimeSpan.FromSeconds(1),
        MinimumThroughput = 5
    };

    public static TheoryData<string> PipelinesWithDefaultConfig => new()
    {
        CompleteTransactionPipelinePath,
        GetPaymentTransactionPipelinePath,
        DchDeletePipelinePath
    };

    [Theory]
    [MemberData(nameof(PipelinesWithDefaultConfig), MemberType = typeof(ActionCircuitBreakerTests))]
    public void GivenServiceStarted_WhenPipelinesWithDefaultPropertiesRetrievedForEndpoints_ThenPipelinesShouldExistWithDefaultConfig(string expectedPipelinePath)
    {
        var pipelineProvider = _fixture.Services.GetRequiredService<ResiliencePipelineProvider<string>>();
        // Arrange

        // Act
        var pipeline = pipelineProvider.GetPipeline(expectedPipelinePath);

        // Assert
        pipeline.Should().NotBeNull();
        var circuitOptions = (CircuitBreakerStrategyOptions)pipeline.GetPipelineDescriptor().FirstStrategy.Options!;

        circuitOptions.FailureRatio.Should().BeApproximately(_expectedDevEnvCircuitOptions.FailureRatio, 0.01f);
        circuitOptions.SamplingDuration.Should().Be(_expectedDevEnvCircuitOptions.SamplingDuration);
        circuitOptions.MinimumThroughput.Should().Be(_expectedDevEnvCircuitOptions.MinimumThroughput);
        circuitOptions.BreakDuration.Should().Be(_expectedDevEnvCircuitOptions.BreakDuration);
    }

    public static TheoryData<string, CircuitBreakerStrategyOptions> SubmitCircuitPipelineEnvironmentSpecificConfig => new()
    {
        { "Development", new CircuitBreakerStrategyOptions { FailureRatio = 0.1, SamplingDuration = TimeSpan.FromSeconds(5), MinimumThroughput = 2, BreakDuration = TimeSpan.FromSeconds(15) } },
        { "Test", new CircuitBreakerStrategyOptions { FailureRatio = 0.1, SamplingDuration = TimeSpan.FromSeconds(5), MinimumThroughput = 2, BreakDuration = TimeSpan.FromSeconds(15) } },
        { "Uat", new CircuitBreakerStrategyOptions { FailureRatio = 0.1, SamplingDuration = TimeSpan.FromSeconds(5), MinimumThroughput = 2, BreakDuration = TimeSpan.FromSeconds(20) } },
        { "Production", new CircuitBreakerStrategyOptions { FailureRatio = 0.2, SamplingDuration = TimeSpan.FromSeconds(5), MinimumThroughput = 5, BreakDuration = TimeSpan.FromSeconds(30) } }
    };

    [Theory]
    [MemberData(nameof(SubmitCircuitPipelineEnvironmentSpecificConfig), MemberType = typeof(ActionCircuitBreakerTests))]
    public void GivenServiceStarted_WhenSubmitPipelineWithEnvSpecificConfigRetrieved_ThenPipelineShouldExistWithCustomConfig(string environmentName, CircuitBreakerStrategyOptions expectedCircuitOptions)
    {
        // Arrange
        var envSpecificFixture = new PortableComponentTestsFixture(environmentName);
        var pipelineProvider = envSpecificFixture.Services.GetRequiredService<ResiliencePipelineProvider<string>>();

        // Act
        var pipeline = pipelineProvider.GetPipeline(SubmitPaymentPipelinePath);

        // Assert
        pipeline.Should().NotBeNull();
        var circuitOptions = (CircuitBreakerStrategyOptions)pipeline.GetPipelineDescriptor().FirstStrategy.Options!;

        // Settings from appSettings.Development.json are used in these component tests, unless overridden
        circuitOptions.FailureRatio.Should().BeApproximately(expectedCircuitOptions.FailureRatio, 0.01f);
        circuitOptions.SamplingDuration.Should().Be(expectedCircuitOptions.SamplingDuration);
        circuitOptions.MinimumThroughput.Should().Be(expectedCircuitOptions.MinimumThroughput);
        circuitOptions.BreakDuration.Should().Be(expectedCircuitOptions.BreakDuration);
    }
}
