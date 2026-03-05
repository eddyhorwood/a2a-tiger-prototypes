using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentExecution.Common.HealthChecks;

public abstract class HttpPingHealthCheck : IHealthCheck
{
    protected abstract string ServiceName { get; init; }
    protected abstract Task<HttpResponseMessage> PerformHttpPingAsync(CancellationToken cancellationToken);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var response = await PerformHttpPingAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            return HealthCheckResult.Healthy($"Successfully pinged {ServiceName}");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy($"Failed to ping {ServiceName}, response status was: {ex.StatusCode}");
        }
        catch
        {
            return HealthCheckResult.Unhealthy($"Failed to ping {ServiceName}");
        }

    }
}
