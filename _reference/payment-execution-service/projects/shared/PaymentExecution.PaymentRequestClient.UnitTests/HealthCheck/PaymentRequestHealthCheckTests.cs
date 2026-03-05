using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using PaymentExecution.PaymentRequestClient.HealthChecks;

namespace PaymentExecution.PaymentRequestClient.UnitTests.HealthCheck;

public class PaymentRequestHealthCheckTests
{
    [Fact]
    public async Task GivenHealthyService_WhenHealthCheckExecuted_ThenHealthyResultExpected()
    {
        // Arrange
        var mockClient = new Mock<IPaymentRequestClient>();
        mockClient.Setup(client => client.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var healthCheck = new PaymentRequestHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Successfully pinged Payment Request Service");
    }

    [Fact]
    public async Task GivenUnhealthyService_WhenHealthCheckExecuted_ThenUnhealthyResultExpected()
    {
        // Arrange
        var mockClient = new Mock<IPaymentRequestClient>();
        mockClient.Setup(client => client.PingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var healthCheck = new PaymentRequestHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Failed to ping Payment Request Service, response status was: InternalServerError");
    }

    [Fact]
    public async Task GivenExceptionOccurs_WhenHealthCheckExecuted_ThenUnhealthyResultExpected()
    {
        // Arrange
        var mockClient = new Mock<IPaymentRequestClient>();
        mockClient.Setup(client => client.PingAsync(It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new System.Exception("Failure within client :("));

        var healthCheck = new PaymentRequestHealthCheck(mockClient.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Failed to ping Payment Request Service");
    }
}
