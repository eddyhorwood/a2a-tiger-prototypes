using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentExecution.Common;
using PaymentExecution.StripeExecutionClient.Contracts.Models;
using PaymentExecution.StripeExecutionClient.Options;
using Polly.Registry;

namespace PaymentExecution.StripeExecutionClient;

public interface IStripeExecutionInternalHttpClient
{
    Task<HttpResponseMessage> SubmitStripeExecutionAsync(StripeExeSubmitPaymentRequestDto submitRequest);
    Task<HttpResponseMessage> CancelStripeExecutionAsync(StripeExeCancelPaymentRequestDto cancelRequest);
    Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets payment intent by payment request ID.
    /// When correlationId and tenantId are provided, headers are added explicitly (for Lambda).
    /// When not provided, relies on header propagation middleware (for API).
    /// </summary>
    Task<HttpResponseMessage> GetPaymentIntentByPaymentRequestIdAsync(
        Guid paymentRequestId,
        Guid? correlationId = null,
        Guid? tenantId = null);
}

public class StripeExecutionInternalHttpClient(
    HttpClient httpClient,
    IOptions<StripeExecutionServiceOptions> stripeExecutionOptions,
    ResiliencePipelineProvider<string> pipelineProvider
) : IStripeExecutionInternalHttpClient
{
    public async Task<HttpResponseMessage> SubmitStripeExecutionAsync(StripeExeSubmitPaymentRequestDto submitRequest)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(StripeExecutionInternalHttpClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(stripeExecutionOptions.Value.SubmitEndpoint, UriKind.Relative),
                Content = new StringContent(JsonSerializer.Serialize(submitRequest), Encoding.UTF8, "application/json")
            };
            return await httpClient.SendAsync(request, cancellationToken);
        });
        return response;
    }

    public async Task<HttpResponseMessage> CancelStripeExecutionAsync(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(StripeExecutionInternalHttpClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var endpoint = stripeExecutionOptions.Value.CancelEndpoint.Replace("{request-id}", cancelRequest.PaymentRequestId.ToString());
            var requestBody = new { cancellationReason = cancelRequest.CancellationReason };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(endpoint, UriKind.Relative),
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            // Add required Xero headers
            request.Headers.Add(ExecutionConstants.XeroCorrelationId, cancelRequest.CorrelationId.ToString());
            request.Headers.Add(ExecutionConstants.XeroTenantId, cancelRequest.TenantId.ToString());

            return await httpClient.SendAsync(request, cancellationToken);
        });
        return response;
    }

    public async Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetAsync("ping", cancellationToken);
    }

    public async Task<HttpResponseMessage> GetPaymentIntentByPaymentRequestIdAsync(
        Guid paymentRequestId,
        Guid? correlationId = null,
        Guid? tenantId = null)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(StripeExecutionInternalHttpClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var endpoint = stripeExecutionOptions.Value.GetPaymentIntentEndpoint.Replace("{payment-request-id}", paymentRequestId.ToString());
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(endpoint, UriKind.Relative)
            };

            AddXeroHeadersIfProvided(request, correlationId, tenantId);

            return await httpClient.SendAsync(request, cancellationToken);
        });

        return response;
    }

    /// <summary>
    /// Adds Xero headers when explicitly provided (for Lambda).
    /// When not provided, header propagation middleware handles it (for API).
    /// </summary>
    private static void AddXeroHeadersIfProvided(HttpRequestMessage request, Guid? correlationId, Guid? tenantId)
    {
        if (correlationId.HasValue)
        {
            request.Headers.Add(ExecutionConstants.XeroCorrelationId, correlationId.Value.ToString());
        }
        if (tenantId.HasValue)
        {
            request.Headers.Add(ExecutionConstants.XeroTenantId, tenantId.Value.ToString());
        }
    }
}
