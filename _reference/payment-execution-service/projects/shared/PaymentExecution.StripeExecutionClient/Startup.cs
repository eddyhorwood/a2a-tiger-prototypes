using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using CollectingPayments.Common.Domain.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PaymentExecution.StripeExecutionClient.Options;
using PaymentExecution.StripeExecutionClient.Resilience;
using Polly;
using Xero.Identity.Integration.Client;

namespace PaymentExecution.StripeExecutionClient;

[ExcludeFromCodeCoverage]
public static class Startup
{
    public static IHttpClientBuilder AddStripeExecutionClient(
        this IServiceCollection services, string clientName,
        IConfiguration configuration)
    {
        services.Configure<StripeExecutionServiceOptions>(
           configuration.GetSection("InternalServices:StripeExecutionService")
        );

        services.AddScoped<Contracts.IStripeExecutionClient, StripeExecutionClient>();
        services.AddOptions<StripeExecutionRetryOptions>().Bind(configuration.GetSection(StripeExecutionRetryOptions.Key));

        services.AddResiliencePipeline(nameof(StripeExecutionInternalHttpClient), (builder, context) =>
        {
            var stripeExecutionRetryOptions = context.ServiceProvider.GetRequiredService<IOptions<StripeExecutionRetryOptions>>().Value;
            StripeExecutionResiliencePipeline.GetStripeExecutionPipeline(builder, stripeExecutionRetryOptions);
        });

        // Register HttpClient with Base URL
        return services.AddHttpClient<IStripeExecutionInternalHttpClient, StripeExecutionInternalHttpClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<StripeExecutionServiceOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add(XeroSoaHeaders.ClientName, clientName);
        }).AddHttpMessageHandler<IdentityTokenRefreshingHandler>();
    }
}
