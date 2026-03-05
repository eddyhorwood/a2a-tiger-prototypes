using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using PaymentExecution.FeatureFlagClient;
using PaymentExecutionService.HealthChecks;
using Xunit;

namespace PaymentExecutionService.UnitTests.HealthChecks;

public class LaunchDarklyHealthCheckTests
{
    [Fact]
    public async Task GivenClientIsInitialized_WhenCheckHealthAsync_ThenReturnHealthy()
    {
        // Arrange
        var mockLdClient = new Mock<IFeatureFlagClient>();
        mockLdClient.Setup(client => client.Initialized).Returns(true);
        var healthCheck = new LaunchDarklyHealthCheck(mockLdClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("LaunchDarkly client is initialized and ready", result.Description);
    }

    [Fact]
    public async Task GivenClientIsNotInitialized_WhenCheckHealthAsync_ThenReturnUnhealthy()
    {
        // Arrange
        var mockLdClient = new Mock<IFeatureFlagClient>();
        mockLdClient.Setup(client => client.Initialized).Returns(false);
        var healthCheck = new LaunchDarklyHealthCheck(mockLdClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("LaunchDarkly client is not initialized", result.Description);
    }

    [Fact]
    public async Task GivenExceptionIsThrown_WhenCheckHealthAsync_ThenReturnUnhealthy()
    {
        // Arrange
        var mockLdClient = new Mock<IFeatureFlagClient>();
        mockLdClient.Setup(client => client.Initialized).Throws(new Exception("Test exception"));
        var healthCheck = new LaunchDarklyHealthCheck(mockLdClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Failed to check LaunchDarkly health status", result.Description);
        Assert.NotNull(result.Exception);
        Assert.Equal("Test exception", result.Exception.Message);
    }
}
