
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecution.Domain.Commands;

namespace CollectingPaymentsExecutionStripePaymentsService.HealthChecks;

public class PaymentExecutionDbHealthCheck(IMediator mediator, ILogger<PaymentExecutionDbHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running health check for Payment Execution DB.");

        var result = await mediator.Send(new DBHealthCheckCommand(), cancellationToken);

        if (result.IsSuccess && result.Value)
        {
            return HealthCheckResult.Healthy("A healthy result.");
        }

        logger.LogError("Health check returned unhealthy for Payment Execution DB.");
        return new HealthCheckResult(context.Registration.FailureStatus, "An unhealthy result.");
    }
}
