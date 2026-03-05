using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentExecutionWorker.Worker.HealthCheck;
public class HealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        /*
        Add logic to evaluate the health of the worker here.
        Because a Kubernetes liveness probe monitors the result of this check, returning an `Unhealthy` or `Degraded`
        result will cause the kubelet to restart the container.
        */
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
