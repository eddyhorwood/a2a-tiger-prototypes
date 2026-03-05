using Asp.Versioning;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Identity.Integration.Client;
using static Xero.Accelerators.Api.Core.Constants;

namespace PaymentExecutionService.Registrations;

public static class HttpRequestRegistration
{
    public static void AddHttpRequestRegistrations(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddXeroIdentityClient();

        // HttpClient uses IdentityTokenRefreshingHandler to call other Xero APIs that are secured by Xero Identity
        webApplicationBuilder.Services.AddHttpClient(Constants.HttpClients.XeroApi)
            .AddHttpMessageHandler<IdentityTokenRefreshingHandler>()
            // Note: Headers must also be included in `IServiceCollection.AddHeaderPropagation` (see Program.cs)
            .AddHeaderPropagation(options =>
            {
                // Propagate Xero-Correlation-Id to correlate requests across multiple components for better observability
                options.Headers.Add(HttpHeaders.XeroCorrelationId);
                // Propagate Xero-User-Id to enable downstream APIs to perform authorisation
                options.Headers.Add(HttpHeaders.XeroUserId);
            });

        webApplicationBuilder.Services.AddPaymentExecutionServiceVersioning();
    }

    public static void AddPactCompatibleServiceVersioning(this IWebHostBuilder builder)
    {
        builder.ConfigureServices(services => services.AddPaymentExecutionServiceVersioning());
    }

    private static void AddPaymentExecutionServiceVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            // Required for un-versioned API endpoints
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader()
            );
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
    }
}
