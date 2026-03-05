using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecution.FeatureFlagClient;

namespace PaymentExecutionService.HealthChecks;

public class LaunchDarklyHealthCheck(IFeatureFlagClient ldClient) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ldClient.Initialized)
            {
                return Task.FromResult(HealthCheckResult.Healthy("LaunchDarkly client is initialized and ready"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("LaunchDarkly client is not initialized"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check LaunchDarkly health status", ex));
        }
    }
}
