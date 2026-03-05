using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.FeatureFlagClient;
using PaymentExecutionService.Middleware;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.UnitTests.Conventions.ErrorHandling;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware;

public class ServiceFeatureFlagFilterTests
{
    private readonly FeatureFlagDefinition<bool> _serviceFlag =
        ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled;

    private readonly MockProblemDetailsFactory _mockProblemDetailsFactory = new();
    private readonly Mock<ResourceExecutionDelegate> _mockNextMiddleware = new();
    private readonly Mock<IFeatureFlagClient> _mockFeatureFlagClient = new();

    [Fact]
    public async Task
        GivenServiceFeatureEnabledInLaunchDarkly_WhenFeatureFilterIsExecuted_ThenRequestContinuesAsExpected()
    {
        // Arrange
        MockFeatureFlag(_mockFeatureFlagClient, _serviceFlag, true);
        var resourceExecutionContext = GetResourceExecutingContext(out var context);

        // Act
        var middleware = new ServiceFeatureFlagFilter(_mockFeatureFlagClient.Object, _mockProblemDetailsFactory);
        var act = async () =>
            await middleware.OnResourceExecutionAsync(resourceExecutionContext, _mockNextMiddleware.Object);

        // Assert
        await act.Should().NotThrowAsync();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        _mockNextMiddleware.Verify(mock => mock.Invoke(), Times.Once);
    }

    [Fact]
    public async Task
        GivenServiceFeatureDisabledInLaunchDarkly_WhenFilterExecutes_ThenRequestIsShortCircuitedAndReturns503()
    {
        // Arrange
        MockFeatureFlag(_mockFeatureFlagClient, _serviceFlag, false);
        var resourceExecutionContext = GetResourceExecutingContext(out var context, true);

        var middleware = new ServiceFeatureFlagFilter(_mockFeatureFlagClient.Object, _mockProblemDetailsFactory);

        // Act
        var act = async () =>
            await middleware.OnResourceExecutionAsync(resourceExecutionContext, _mockNextMiddleware.Object);

        // Assert
        await act.Should().NotThrowAsync();
        context.Response.StatusCode.Should().Be(503);
        _mockNextMiddleware.Verify(mock => mock.Invoke(), Times.Never);
    }

    private static ResourceExecutingContext GetResourceExecutingContext(out HttpContext context,
        bool includeServiceCollection = false)
    {
        if (!includeServiceCollection)
        {
            context = new DefaultHttpContext();
        }
        else
        {
            var services = new ServiceCollection();
            var serviceProvider = services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
            }).BuildServiceProvider();
            context = new DefaultHttpContext { RequestServices = serviceProvider };
        }

        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(actionContext, new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
    }

    private static void MockFeatureFlag(Mock<IFeatureFlagClient> client, FeatureFlagDefinition<bool> flag, bool value)
    {
        client.Setup(mock => mock.GetFeatureFlag(flag, null))
            .Returns(
                new FeatureFlag<bool>() { Name = flag.Name, Value = value }
            );
    }
}
