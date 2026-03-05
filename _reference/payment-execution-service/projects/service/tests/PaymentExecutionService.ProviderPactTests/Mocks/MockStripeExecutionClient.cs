using System.Net;
using System.Text.Json;
using PaymentExecution.StripeExecutionClient;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecutionService.ProviderPactTests.Mocks;

public class MockStripeExecutionInternalHttpClient : IStripeExecutionInternalHttpClient
{
    public Task<HttpResponseMessage> SubmitStripeExecutionAsync(StripeExeSubmitPaymentRequestDto submitRequest)
    {
        var mockResponseDict = new Dictionary<string, string>
        {
            { "paymentIntentId", "pi_lol" },
            { "clientSecret", "pi_client_secret" },
            { "providerServiceId", Guid.NewGuid().ToString() }
        };
        var result = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(mockResponseDict))
        };
        return Task.FromResult(result);
    }

    public Task<HttpResponseMessage> CancelStripeExecutionAsync(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        return Task.FromResult(new HttpResponseMessage());
    }

    public Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK
        };
        return Task.FromResult(response);
    }

    public Task<HttpResponseMessage> GetPaymentIntentByPaymentRequestIdAsync(Guid paymentRequestId, Guid? correlationId = null,
    Guid? tenantId = null)
    {
        return Task.FromResult(new HttpResponseMessage());
    }
}
