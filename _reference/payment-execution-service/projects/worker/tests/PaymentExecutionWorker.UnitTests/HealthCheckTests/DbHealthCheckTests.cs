using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Commands;
using PaymentExecutionWorker.Worker.HealthCheck;

namespace PaymentExecutionWorker.UnitTests.HealthCheckTests;

public class DbHealthCheckTests
{

    [Fact]
    public async Task GivenDbIsHealthy_WhenCheckHealthAsyncIsCalled_ReturnsHealthy()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<DbHealthCheck>>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<DBHealthCheckCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(true));

        // act
        var healthCheck = new DbHealthCheck(mediatorMock.Object, loggerMock.Object);
        var context = new HealthCheckContext();
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GivenDbIsUnhealthy_WhenCheckHealthAsyncIsCalled_ReturnsUnhealthy()
    {
        // arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<DbHealthCheck>>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<DBHealthCheckCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Database error"));

        // act
        var healthCheck = new DbHealthCheck(mediatorMock.Object, loggerMock.Object);
        var context = new HealthCheckContext();
        var result = await healthCheck.CheckHealthAsync(context);

        // assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public Task GivenResultReturnValueIsFalse_WhenCheckHealthAsyncIsCalled_ReturnsUnhealthy()
    {
        // arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<DbHealthCheck>>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<DBHealthCheckCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(false));

        // act
        var healthCheck = new DbHealthCheck(mediatorMock.Object, loggerMock.Object);
        var context = new HealthCheckContext();
        var result = healthCheck.CheckHealthAsync(context);

        // assert
        return result.ContinueWith(t => t.Result.Status.Should().Be(HealthStatus.Unhealthy));
    }
}
