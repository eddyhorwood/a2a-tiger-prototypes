using PaymentExecution.Common;

namespace PaymentExecution.TestUtilities;

public static class PactConsumerClientFactory
{
    public static HttpClient CreateHttpClient(Uri baseAddress,
        string? tenantId = null,
        string? correlationId = null,
        string? authorization = null)
    {
        var client = new HttpClient { BaseAddress = baseAddress };
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            client.DefaultRequestHeaders.Add(ExecutionConstants.XeroTenantId, tenantId);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            client.DefaultRequestHeaders.Add(ExecutionConstants.XeroCorrelationId, correlationId);
        }

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            client.DefaultRequestHeaders.Add(ExecutionConstants.Authorization, authorization);
        }

        return client;
    }
}
