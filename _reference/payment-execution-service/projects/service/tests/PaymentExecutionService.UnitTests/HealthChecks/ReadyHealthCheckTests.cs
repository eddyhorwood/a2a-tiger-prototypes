using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecutionService.HealthChecks;
using Xunit;

namespace PaymentExecutionService.UnitTests.HealthChecks;

public class ReadyHealthCheckTests
{
    [Fact]
    public async Task GivenReadyHealthCheck_WhenDependencyIsHealthy_ReturnsHealthy()
    {
        // Arrange
        var check = new ReadyHealthCheck();
        var context = new HealthCheckContext();
        var token = CancellationToken.None;

        // Act
        var result = await check.CheckHealthAsync(context, token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
