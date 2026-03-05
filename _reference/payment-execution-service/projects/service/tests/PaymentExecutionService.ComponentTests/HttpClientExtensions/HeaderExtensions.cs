using System;
using System.Net.Http;
namespace PaymentExecutionService.ComponentTests.HttpClientExtensions;

public static class HeaderExtensions
{
    public static HttpClient WithProviderAccountId(this HttpClient client, string? providerAccountId = "valid-provider-account-id")
    {
        client.DefaultRequestHeaders.Add(PaymentExecutionService.Constants.HttpHeaders.ProviderAccountId, providerAccountId);
        return client;
    }

    public static HttpClient WithXeroTenantId(this HttpClient client, Guid? tenantId = null)
    {
        client.DefaultRequestHeaders.Add(Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId,
            tenantId != null ? tenantId.ToString() : Guid.NewGuid().ToString());
        return client;
    }
}
