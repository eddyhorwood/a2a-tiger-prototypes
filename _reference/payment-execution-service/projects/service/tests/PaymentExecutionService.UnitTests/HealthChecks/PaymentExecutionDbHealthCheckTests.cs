using System.Threading;
using System.Threading.Tasks;
using CollectingPaymentsExecutionStripePaymentsService.HealthChecks;
using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Commands;
using Xunit;

namespace PaymentExecutionService.UnitTests.HealthChecks;

public class PaymentExecutionDbHealthCheckTests
{
    private static readonly string[] _healthTag = new[] { "health" };
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<PaymentExecutionDbHealthCheck>> _mockedLogger = new();
    private readonly PaymentExecutionDbHealthCheck _sut;

    public PaymentExecutionDbHealthCheckTests()
    {
        _sut = new PaymentExecutionDbHealthCheck(_mediator.Object, _mockedLogger.Object);
    }

    [Fact]
    public async Task GivenDependencyIsHealthy_WhenPaymentExecutionDbHealthCheck_ThenReturnsHealthy()
    {
        var context = new HealthCheckContext();
        var token = CancellationToken.None;
        _mediator.Setup(m => m.Send(It.IsAny<DBHealthCheckCommand>(), token))
            .ReturnsAsync(Result.Ok(true));

        var result = await _sut.CheckHealthAsync(context, token);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GivenDependencyIsUnHealthy_WhenPaymentExecutionDbHealthCheck_ThenReturnsUnHealthy()
    {
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("PaymentExecutionDBHealthCheck", _sut, HealthStatus.Unhealthy, tags: _healthTag)
        };
        var expectedFailureDescription = "An unhealthy result.";
        var token = CancellationToken.None;
        _mediator.Setup(m => m.Send(It.IsAny<DBHealthCheckCommand>(), token))
            .ReturnsAsync(Result.Ok(false));

        var result = await _sut.CheckHealthAsync(context, token);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be(expectedFailureDescription);
    }
}
