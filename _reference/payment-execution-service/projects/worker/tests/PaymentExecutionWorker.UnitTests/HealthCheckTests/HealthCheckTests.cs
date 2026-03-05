using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecutionWorker.Worker.HealthCheck;

namespace PaymentExecutionWorker.UnitTests.HealthCheckTests;

public class HealthCheckTests
{
    [Fact]
    public async Task GivenHealthCheck_WhenDependencyIsHealthy_ThenReturnsHealthy()
    {
        // Arrange
        var check = new HealthCheck();
        var context = new HealthCheckContext();

        // Act
        var result = await check.CheckHealthAsync(context, new CancellationToken());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
