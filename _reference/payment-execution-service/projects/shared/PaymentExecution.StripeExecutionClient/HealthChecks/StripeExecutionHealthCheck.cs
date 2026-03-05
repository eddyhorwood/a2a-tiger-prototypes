using PaymentExecution.Common.HealthChecks;

namespace PaymentExecution.StripeExecutionClient.HealthChecks;

public class StripeExecutionHealthCheck(IStripeExecutionInternalHttpClient internalHttpClient) : HttpPingHealthCheck
{
    protected override string ServiceName { get; init; } = "Stripe Execution Service";
    protected override async Task<HttpResponseMessage> PerformHttpPingAsync(CancellationToken cancellationToken)
    {
        return await internalHttpClient.PingAsync(cancellationToken);
    }
}
