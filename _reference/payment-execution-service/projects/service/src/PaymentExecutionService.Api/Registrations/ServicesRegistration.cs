using Microsoft.AspNetCore.Authorization;
using PaymentExecution.Domain;
using PaymentExecution.Domain.Service;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.NewRelicClient;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.Repository;
using PaymentExecution.SqsIntegrationClient;
using PaymentExecution.StripeExecutionClient;
using PaymentExecutionService.Mapping;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using PaymentExecutionService.Models;

namespace PaymentExecutionService.Registrations;

public static class ServicesRegistration
{
    private const string ClientName = "Payment-Execution-Service";
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigMediatR(configuration)
            .AddDbIntegrationServices(configuration)
            .AddSqsIntegration(configuration)
            .AddNewRelicClient()
            .AddAutoMapper(
                typeof(ControllerToDomainMappingProfile),
                typeof(DomainToControllerMappingProfile)
            )
            .AddLaunchDarkly(configuration)
            .AddAuthorizationBuilder()
            .AddPolicy(Constants.ServiceAuthorizationPolicies.ReadOnly,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.ReadOnly))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.DchDelete,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.DchDelete))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.Submit,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.Submit))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.Complete,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.Complete))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.RequestCancel,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.RequestCancel))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.Cancel,
                policy => policy.RequireScope(Constants.ServiceAuthorizationScopes.Cancel))
            .AddPolicy(Constants.ServiceAuthorizationPolicies.ReadProviderState,
                policy => policy.RequireScope(
                    Constants.ServiceAuthorizationScopes.ReadProviderState));
        services.AddPaymentRequestClient(ClientName, configuration)
            .AddHeaderPropagation();
        services.AddStripeExecutionClient(ClientName, configuration).AddHeaderPropagation();
        services.AddServiceWhitelistOptions(configuration);
        services.AddScoped<IRequestCancelDomainService, RequestCancelDomainService>();
        services.AddCancellationValidationService();
        services.AddProviderIntegrationServices();

        //TEMPORARY: To be removed as part of INPAY-20744
        services.AddScoped<IProcessCompleteMessageDomainService, ProcessCompleteMessageDomainService>();

        services.RegisterEndpointCircuitPipelines(configuration);
    }
}
