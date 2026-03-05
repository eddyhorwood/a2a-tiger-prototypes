using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using CollectingPayments.Common.Domain.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PaymentExecution.PaymentRequestClient.Resilience;
using Polly;
using Xero.Identity.Integration.Client;

namespace PaymentExecution.PaymentRequestClient;

[ExcludeFromCodeCoverage]
public static class Startup
{
    public static IHttpClientBuilder AddPaymentRequestClient(
        this IServiceCollection services, string clientName,
        IConfiguration configuration)
    {
        services.Configure<PaymentRequestServiceOptions>(
            configuration.GetSection("InternalServices:PaymentRequestService")
        );

        services.AddOptions<PaymentRequestRetryOptions>().Bind(configuration.GetSection(PaymentRequestRetryOptions.Key));

        services.AddResiliencePipeline(nameof(PaymentRequestClient), (builder, context) =>
        {
            var paymentRequestRetryOptions = context.ServiceProvider.GetRequiredService<IOptions<PaymentRequestRetryOptions>>().Value;
            PaymentRequestResiliencePipeline.GetPaymentRequestPipeline(builder, paymentRequestRetryOptions);
        });

        // Register HttpClient with Base URL
        return services.AddHttpClient<IPaymentRequestClient, PaymentRequestClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PaymentRequestServiceOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add(XeroSoaHeaders.ClientName, clientName);
        }).AddHttpMessageHandler<IdentityTokenRefreshingHandler>();
    }
}
