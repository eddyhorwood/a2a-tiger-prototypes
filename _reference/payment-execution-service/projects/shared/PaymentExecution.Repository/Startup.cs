using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentExecution.Repository;

public static class Startup
{
    public static IServiceCollection AddDbIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IConnectionFactory, ConnectionFactory>();
        services.Configure<PaymentExecutionDbConnectionOptions>(configuration.GetSection(PaymentExecutionDbConnectionOptions.Key));
        services.AddScoped<IPaymentExecutionDbConnection, PaymentExecutionDbConnection>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IDapperWrapper, DapperWrapper>();
        return services;
    }
}
