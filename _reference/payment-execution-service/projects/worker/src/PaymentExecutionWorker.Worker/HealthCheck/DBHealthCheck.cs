using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecution.Domain.Commands;

namespace PaymentExecutionWorker.Worker.HealthCheck;

public class DbHealthCheck : IHealthCheck
{
    private readonly IMediator _mediator;
    private readonly ILogger<DbHealthCheck> _logger;

    public DbHealthCheck(IMediator mediator, ILogger<DbHealthCheck> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running health check for Payment Execution Worker DB.");

        var result = await _mediator.Send(new DBHealthCheckCommand(), cancellationToken);

        if (result.IsSuccess && result.Value)
        {
            return HealthCheckResult.Healthy("A healthy result.");
        }

        _logger.LogError("Health check returned unhealthy for Payment Execution Worker DB.");
        return HealthCheckResult.Unhealthy("An unhealthy result.");
    }
}
