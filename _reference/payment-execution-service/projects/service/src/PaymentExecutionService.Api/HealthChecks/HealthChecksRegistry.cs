// Learn more about Health Checks at https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-8.0

using CollectingPaymentsExecutionStripePaymentsService.HealthChecks;
using PaymentExecution.PaymentRequestClient.HealthChecks;
using PaymentExecution.StripeExecutionClient.HealthChecks;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Identity.Authentication.Events.Testing;
using Xero.Identity.Authentication.HealthChecks;

namespace PaymentExecutionService.HealthChecks;

public static class HealthChecksRegistry
{
    private static readonly string[] _readyTag = ["ready"];
    private static readonly string[] _healthTag = ["health"];

    public static void AddServiceHealthChecks(this WebApplicationBuilder webApplicationBuilder)
    {
        var identityConfiguration = webApplicationBuilder.Configuration.GetSection(IdentityOptions.Key);

        webApplicationBuilder.Services.AddHealthChecks()
            .AddCheck<ReadyHealthCheck>("Ready", tags: _readyTag)
            .AddCheck<PaymentExecutionDbHealthCheck>("PaymentExecutionDBHealthCheck", tags: _healthTag)
            .AddCheck<PaymentExecutionDbHealthCheck>("PaymentExecutionDBReadinessCheck", tags: _readyTag)
            .AddCheck<LaunchDarklyHealthCheck>("LaunchDarklyCheckReadinessCheck", tags: _readyTag)
            .AddCheck<StripeExecutionHealthCheck>("StripeExecutionPingCheck", tags: _healthTag)
            .AddCheck<PaymentRequestHealthCheck>("PaymentRequestPingCheck", tags: _healthTag)
            .AddXeroIdentityHealthChecks(identityConfiguration, tags: _healthTag);

        if (webApplicationBuilder.Environment.IsDevelopment())
        {
            // Disable Identity metrics events
            webApplicationBuilder.Services.AddXeroIdentityAuthenticationEventsTesting();
        }
    }
}
