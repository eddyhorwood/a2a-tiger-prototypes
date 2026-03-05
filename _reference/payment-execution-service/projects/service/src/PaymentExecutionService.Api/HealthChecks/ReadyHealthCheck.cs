using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentExecutionService.HealthChecks;

public class ReadyHealthCheck() : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // ... run the health check here.
        // remember to respect the cancellationToken.
        // This might include calling an external service.
        return Task.FromResult(HealthCheckResult.Healthy("A healthy result."));
    }
}
