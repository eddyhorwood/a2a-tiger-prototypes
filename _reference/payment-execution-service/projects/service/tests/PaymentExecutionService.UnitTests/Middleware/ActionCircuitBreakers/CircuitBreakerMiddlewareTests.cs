using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware.ActionCircuitBreakers;

public class AssertionStrategyOptions : ResilienceStrategyOptions
{
    public bool HasExecuted { get; set; }
}

public class AssertionStrategy : ResilienceStrategy
{
    private readonly AssertionStrategyOptions _options;
    private readonly Exception? _thrownException;
    public AssertionStrategy(AssertionStrategyOptions options, Exception? pipelineExceptionToThrow = null)
    {
        _options = options;
        _thrownException = pipelineExceptionToThrow ?? null;
    }

    protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
        ResilienceContext context,
        TState state)
    {
        _options.HasExecuted = true;
        if (_thrownException != null)
        {
            throw _thrownException;
        }
        return await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
    }
}

public class CircuitBreakerMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ResiliencePipelineProvider<string>> _pipelineProviderMock;
    private readonly CircuitMiddleware _middleware;
    private readonly Mock<IProblemDetailsService> _problemDetailsService;
    private readonly AssertionStrategyOptions _assertionStrategyOptions;
    private readonly Mock<ILogger<CircuitMiddleware>> _loggerMock;

    public CircuitBreakerMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<CircuitMiddleware>>();
        _pipelineProviderMock = new Mock<ResiliencePipelineProvider<string>>();
        _middleware = new CircuitMiddleware(_nextMock.Object, _loggerMock.Object, _pipelineProviderMock.Object);
        _problemDetailsService = new Mock<IProblemDetailsService>();
        _assertionStrategyOptions = new AssertionStrategyOptions();
    }

    [Fact]
    public async Task GivenPipelineForActionExistsInRegistry_WhenInvokeAsync_ThenSpecificPipelineIsExecuted()
    {
        // Arrange
        var context = SetupDummyTestActionContext();

        var pipeline = SetupTestPipeline();

        _pipelineProviderMock
            .Setup(p => p.TryGetPipeline("v1.0:TestController:TestAction", out pipeline))
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _assertionStrategyOptions.HasExecuted.Should().Be(true);
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task GivenPipelineForActionThrowsBrokenCircuitException_WhenInvokeAsync_ThenResponseIncludesUsefulInformationAboutCircuit()
    {
        // Arrange
        var context = SetupDummyTestActionContext();

        var expectedBrokenCircuitException = new BrokenCircuitException("circuit open", TimeSpan.FromSeconds(23));
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Circuit open error",
            Detail = $"Retry available in {expectedBrokenCircuitException.RetryAfter!.Value.TotalSeconds} seconds.",
            Type = "https://common.service.xero.com/schema/problems/circuit-breaker-open",
            Extensions = new Dictionary<string, object?>()
            {
                { "retryAfter", expectedBrokenCircuitException.RetryAfter!.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture) }
            },
            ErrorCode = ErrorConstants.ErrorCode.GenericExecutionError,
            ProviderErrorCode = null
        };

        var pipeline = SetupTestPipeline(expectedBrokenCircuitException);

        _pipelineProviderMock
            .Setup(p => p.TryGetPipeline("v1.0:TestController:TestAction", out pipeline))
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _assertionStrategyOptions.HasExecuted.Should().Be(true);
        _nextMock.Verify(next => next(context), Times.Never);
        _problemDetailsService.Verify(m => m.WriteAsync(It.Is<ProblemDetailsContext>(details =>
            details.ProblemDetails.Detail == expectedProblemDetails.Detail &&
            details.ProblemDetails.Status == expectedProblemDetails.Status &&
            (string)details.ProblemDetails.Extensions["retryAfter"]! == expectedBrokenCircuitException.RetryAfter.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture) &&
            ((ProblemDetailsExtended)details.ProblemDetails).ErrorCode == expectedProblemDetails.ErrorCode &&
            ((ProblemDetailsExtended)details.ProblemDetails).ProviderErrorCode == expectedProblemDetails.ProviderErrorCode
            )), Times.Once);
    }

    [Fact]
    public async Task GivenPipelineForActionThrowsBrokenCircuitExceptionWithoutRetryAfterInfo_WhenInvokeAsync_ThenResponseStillShowsErrorAndNoPropertyAccessErrors()
    {
        // Arrange
        var context = SetupDummyTestActionContext();

        var brokenCircuitExceptionWithoutRetryInfo = new BrokenCircuitException();
        var expectedProblemDetails = new ProblemDetails()
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Circuit open error",
            Detail = $"Retry available in unknown seconds.",
            Type = "https://common.service.xero.com/schema/problems/circuit-breaker-open",
            Extensions = new Dictionary<string, object?>()
            {
                { "retryAfter", "unknown" }
            }
        };

        var pipeline = SetupTestPipeline(brokenCircuitExceptionWithoutRetryInfo);

        _pipelineProviderMock
            .Setup(p => p.TryGetPipeline("v1.0:TestController:TestAction", out pipeline))
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _assertionStrategyOptions.HasExecuted.Should().Be(true);
        _nextMock.Verify(next => next(context), Times.Never);
        _problemDetailsService.Verify(m => m.WriteAsync(It.Is<ProblemDetailsContext>(details =>
            details.ProblemDetails.Detail == expectedProblemDetails.Detail &&
            details.ProblemDetails.Status == expectedProblemDetails.Status &&
            (string)details.ProblemDetails.Extensions["retryAfter"]! == "unknown" &&
            ((ProblemDetailsExtended)details.ProblemDetails).ErrorCode == ErrorConstants.ErrorCode.GenericExecutionError &&
            ((ProblemDetailsExtended)details.ProblemDetails).ProviderErrorCode == null
            )), Times.Once);
    }

    [Theory]
    [InlineData("controller")]
    [InlineData("action")]
    public async Task GivenMissingRouteValue_WhenInvokeAsync_ThenNoPipelineIsUsed(string missingKey)
    {
        // Arrange
        var context = SetupDummyTestActionContext();

        context.Request.RouteValues.Remove(missingKey);

        var pipeline = SetupTestPipeline();
        _pipelineProviderMock
            .Setup(p => p.TryGetPipeline(It.IsAny<string>(), out pipeline))
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _assertionStrategyOptions.HasExecuted.Should().Be(false);
        _nextMock.Verify(next => next(context), Times.Once);
    }

    private DefaultHttpContext SetupDummyTestActionContext(ApiVersion? endpointVersion = null)
    {
        var services = new ServiceCollection();
        var context = new DefaultHttpContext();
        services.AddSingleton(_problemDetailsService.Object);
        services.AddApiVersioning();
        IApiVersioningFeature feature = new ApiVersioningFeature(context)
        {
            RequestedApiVersion = endpointVersion ?? new ApiVersion(1, 0)
        };
        var mockServiceProvider = services.BuildServiceProvider();
        context.RequestServices = mockServiceProvider;
        context.Features.Set(feature);

        context.Request.RouteValues.Add("controller", "Test");
        context.Request.RouteValues.Add("action", "TestAction");

        return context;
    }

    private ResiliencePipeline SetupTestPipeline(BrokenCircuitException? expectedBrokenCircuitException = null)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddStrategy(_ => new AssertionStrategy(_assertionStrategyOptions, expectedBrokenCircuitException))
            .Build();
        return pipeline;
    }
}
