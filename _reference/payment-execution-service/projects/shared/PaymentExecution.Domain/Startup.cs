using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Service;
using PaymentExecution.Domain.Service.Strategies;
using PaymentExecution.Domain.Specifications.CancellationSpecification;

namespace PaymentExecution.Domain;

[ExcludeFromCodeCoverage]
public static class Startup
{
    public static IServiceCollection ConfigMediatR(this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddScoped<ISubmitStripePaymentDomainService, SubmitStripePaymentDomainService>();
        services.AddScoped<IGetProviderStateDomainService, GetProviderStateDomainService>();
        services.AddScoped<ICancelDomainService, CancelDomainService>();
        services.AddAutoMapper(typeof(DomainToRepositoryMappingProfile));
        services.AddAutoMapper(typeof(DomainToDomainMappingProfile));
        services.AddAutoMapper(typeof(DomainToServiceMappingProfile));
        services.AddAutoMapper(typeof(RepositoryToDomainMappingProfile));
        services.AddAutoMapper(typeof(ServiceToDomainMappingProfile));
        services.AddHttpContextAccessor();
        return services;
    }

    public static IServiceCollection AddCancellationValidationService(this IServiceCollection services)
    {
        services.AddScoped<ICancellationValidationService, CancellationValidationService>();

        services.AddScoped<ICancellationSpecification, EligibleStatusSpecification>();
        services.AddScoped<ICancellationSpecification, SupportedProviderSpecification>();
        services.AddScoped<ICancellationSpecification, ProviderPaymentTransactionIdSpecification>();
        return services;
    }

    public static IServiceCollection AddProviderIntegrationServices(this IServiceCollection services)
    {
        services.AddScoped<IProviderIntegrationDomainServiceFactory, ProviderIntegrationDomainServiceFactory>();

        //Stripe
        services.AddScoped<IProviderIntegrationDomainService, StripeIntegrationDomainService>();
        //Strategies for supported pending actions for stripe
        services.AddScoped<IStripePendingActionStrategy, BankTransferStrategy>();
        services.AddScoped<IStripePendingActionStrategy, RedirectToUrlStrategy>();
        services.AddScoped<IStripePendingActionStrategy, MicrodepositVerificationStrategy>();
        services.AddScoped<IStripePendingActionStrategy, PayToAuthorizationStrategy>();

        return services;
    }
}
