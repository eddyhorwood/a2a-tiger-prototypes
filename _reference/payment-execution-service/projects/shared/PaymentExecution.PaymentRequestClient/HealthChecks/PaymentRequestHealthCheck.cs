using PaymentExecution.Common.HealthChecks;

namespace PaymentExecution.PaymentRequestClient.HealthChecks;

public class PaymentRequestHealthCheck(IPaymentRequestClient client) : HttpPingHealthCheck
{
    protected override string ServiceName { get; init; } = "Payment Request Service";
    protected override async Task<HttpResponseMessage> PerformHttpPingAsync(CancellationToken cancellationToken)
    {
        return await client.PingAsync(cancellationToken);
    }
}
