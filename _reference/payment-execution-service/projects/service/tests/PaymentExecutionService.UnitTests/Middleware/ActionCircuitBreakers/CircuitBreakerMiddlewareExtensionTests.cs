using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using PaymentExecution.NewRelicClient;
using PaymentExecution.TestUtilities;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware.ActionCircuitBreakers;

public class CircuitBreakerMiddlewareExtensionTests
{
    [Fact]
    public async Task GivenValidConfiguration_WhenBinding_ThenOptionsShouldBeCorrectlySet()
    {
        // Arrange
        var expectedFailureRatio = 0.42;
        var expectedSamplingDurationSeconds = 42;
        var expectedMinimumThroughput = 84;
        var expectedBreakDurationSeconds = 0.84;
        var inMemorySettings = new Dictionary<string, string>
        {
            { "ResilienceConfigurations:EndpointCircuits:configPath:FailureRatio", expectedFailureRatio.ToString(CultureInfo.InvariantCulture) },
            { "ResilienceConfigurations:EndpointCircuits:configPath:SamplingDurationSeconds", expectedSamplingDurationSeconds.ToString(CultureInfo.InvariantCulture) },
            { "ResilienceConfigurations:EndpointCircuits:configPath:MinimumThroughput", expectedMinimumThroughput.ToString(CultureInfo.InvariantCulture) },
            { "ResilienceConfigurations:EndpointCircuits:configPath:BreakDurationSeconds", expectedBreakDurationSeconds.ToString(CultureInfo.InvariantCulture) }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        var mockMonitoringClient = new Mock<IMonitoringClient>();
        services.AddSingleton<IMonitoringClient>(_ => mockMonitoringClient.Object);
        services.AddEndpointCircuitBreakerPipeline(configuration, "configPath");
        var serviceProvider = services.BuildServiceProvider();

        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();
        // Act & Assert
        var pipelineUnderTest = pipelineProvider.GetPipeline("configPath");
        Assert.NotNull(pipelineUnderTest);

        var circuitStrategy = pipelineUnderTest.GetPipelineDescriptor().FirstStrategy;
        var circuitOptions = Assert.IsType<CircuitBreakerStrategyOptions>(circuitStrategy.Options);
        circuitOptions.FailureRatio.Should().BeApproximately(expectedFailureRatio, 0.01f);
        circuitOptions.SamplingDuration.Should().Be(TimeSpan.FromSeconds(expectedSamplingDurationSeconds));
        circuitOptions.MinimumThroughput.Should().Be(expectedMinimumThroughput);
        circuitOptions.BreakDuration.Should().Be(TimeSpan.FromSeconds(expectedBreakDurationSeconds));

        var context = ResilienceContextPool.Shared.Get();
        var httpReqExnArgs = new CircuitBreakerPredicateArguments<object>(
            context,
            Outcome.FromException<object>(new HttpRequestException("Test HttpRequestException")));
        Assert.True(await circuitOptions.ShouldHandle(httpReqExnArgs));

        var timeoutExnArgs = new CircuitBreakerPredicateArguments<object>(
            context,
            Outcome.FromException<object>(new TimeoutException("Test TimeoutException")));
        Assert.True(await circuitOptions.ShouldHandle(timeoutExnArgs));

        var mockNpgsqlTransientException = new Mock<NpgsqlException>();
        mockNpgsqlTransientException.SetupGet(e => e.IsTransient).Returns(true);

        var npgsqlExceptionTransientArgs = new CircuitBreakerPredicateArguments<object>(
            context,
            Outcome.FromException<object>(mockNpgsqlTransientException.Object));
        Assert.True(await circuitOptions.ShouldHandle(npgsqlExceptionTransientArgs));

        // Generic exception that is not explicitly stated in the configuration - should not be handled
        var nullRefExnArgs = new CircuitBreakerPredicateArguments<object>(
            context,
            Outcome.FromException<object>(new NullReferenceException("Test NullReferenceException")));
        Assert.False(await circuitOptions.ShouldHandle(nullRefExnArgs));

        // Should not react to a NpgsqlException with IsTransient=false
        var mockNpgsqlNonTransientException = new Mock<NpgsqlException>();
        mockNpgsqlNonTransientException.SetupGet(e => e.IsTransient).Returns(false);

        var npgsqlExceptionNonTransientArgs = new CircuitBreakerPredicateArguments<object>(
            context,
            Outcome.FromException<object>(mockNpgsqlNonTransientException.Object));
        Assert.False(await circuitOptions.ShouldHandle(npgsqlExceptionNonTransientArgs));
    }

    [Fact]
    public async Task GivenValidConfiguration_WhenCircuitOpenCallbackInvoked_ThenErrorShouldBeLoggedAndMonitoringInformed()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CircuitMiddleware>>();
        var mockMonitoringClient = new Mock<IMonitoringClient>();
        var pipelineUnderTest = CreatePipelineUnderTest(mockLogger, mockMonitoringClient);

        var circuitStrategy = pipelineUnderTest.GetPipelineDescriptor().FirstStrategy;
        var circuitOptions = Assert.IsType<CircuitBreakerStrategyOptions>(circuitStrategy.Options);
        var stubContext = ResilienceContextPool.Shared.Get(new CancellationToken());

        // Act
        var onOpenedFunc = Assert.IsType<Func<OnCircuitOpenedArguments<object>, ValueTask>>(circuitOptions.OnOpened);
        await onOpenedFunc.Invoke(new OnCircuitOpenedArguments<object>(stubContext,
            new Outcome<object>(), TimeSpan.FromSeconds(30), false));

        // Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(mockLogger, LogLevel.Error, "Circuit 'configPath' opened. Break duration: 00:00:30", 1);
        mockMonitoringClient.Verify(mock => mock.NotifyNewRelicOfCircuitEvent(CircuitState.Open, "configPath", It.Is<TimeSpan>(t => t == TimeSpan.FromSeconds(30))), Times.Once);
    }

    [Fact]
    public async Task GivenValidConfiguration_WhenCircuitClosed_ThenInfoShouldBeLoggedAndMonitoringInformed()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CircuitMiddleware>>();
        var mockMonitoringClient = new Mock<IMonitoringClient>();
        var pipelineUnderTest = CreatePipelineUnderTest(mockLogger, mockMonitoringClient);

        var circuitStrategy = pipelineUnderTest.GetPipelineDescriptor().FirstStrategy;
        var circuitOptions = Assert.IsType<CircuitBreakerStrategyOptions>(circuitStrategy.Options);
        var stubContext = ResilienceContextPool.Shared.Get(new CancellationToken());

        // Act
        var onClosedFunc = Assert.IsType<Func<OnCircuitClosedArguments<object>, ValueTask>>(circuitOptions.OnClosed);
        await onClosedFunc.Invoke(new OnCircuitClosedArguments<object>(stubContext,
            new Outcome<object>(), false));

        // Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(mockLogger, LogLevel.Information, "Circuit 'configPath' closed", 1);
        mockMonitoringClient.Verify(mock => mock.NotifyNewRelicOfCircuitEvent(CircuitState.Closed, "configPath", null), Times.Once);

    }

    [Fact]
    public async Task GivenValidConfiguration_WhenCircuitHalfOpened_ThenInfoShouldBeLoggedAndMonitoringInformed()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CircuitMiddleware>>();
        var mockMonitoringClient = new Mock<IMonitoringClient>();
        var pipelineUnderTest = CreatePipelineUnderTest(mockLogger, mockMonitoringClient);

        var circuitStrategy = pipelineUnderTest.GetPipelineDescriptor().FirstStrategy;
        var circuitOptions = Assert.IsType<CircuitBreakerStrategyOptions>(circuitStrategy.Options);
        var stubContext = ResilienceContextPool.Shared.Get(new CancellationToken());

        // Act
        var onHalfOpenedFunc = Assert.IsType<Func<OnCircuitHalfOpenedArguments, ValueTask>>(circuitOptions.OnHalfOpened);
        await onHalfOpenedFunc.Invoke(new OnCircuitHalfOpenedArguments(stubContext));

        // Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(mockLogger, LogLevel.Information, "Circuit 'configPath' half-opened for probe query.", 1);
        mockMonitoringClient.Verify(mock => mock.NotifyNewRelicOfCircuitEvent(CircuitState.HalfOpen, "configPath", null), Times.Once);
    }

    private static ResiliencePipeline CreatePipelineUnderTest(Mock<ILogger<CircuitMiddleware>> mockLogger, Mock<IMonitoringClient> mockMonitoringClient)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger<CircuitMiddleware>>(_ => mockLogger.Object);
        services.AddSingleton<IMonitoringClient>(_ => mockMonitoringClient.Object);

        var configuration = new ConfigurationBuilder()
            .Build();
        services.AddEndpointCircuitBreakerPipeline(configuration, "configPath");
        var serviceProvider = services.BuildServiceProvider();

        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();
        var pipelineUnderTest = pipelineProvider.GetPipeline("configPath");
        return pipelineUnderTest;
    }
}
