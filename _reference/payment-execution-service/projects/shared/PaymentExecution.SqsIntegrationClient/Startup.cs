using System.Runtime.CompilerServices;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.SqsIntegrationClient.Client;
using PaymentExecution.SqsIntegrationClient.Options;
using PaymentExecution.SqsIntegrationClient.Service;

[assembly: InternalsVisibleTo("PaymentExecution.SqsIntegrationClient.UnitTests")]
namespace PaymentExecution.SqsIntegrationClient;

public static class Startup
{
    public static IServiceCollection AddSqsIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExecutionQueueOptions>(configuration.GetSection(ExecutionQueueOptions.Key));
        services.Configure<CancelExecutionQueueOptions>(configuration.GetSection(CancelExecutionQueueOptions.Key));
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonSQS>();
        services.AddSingleton<IExecutionQueueService, ExecutionQueueService>();
        services.AddSingleton<ICancelExecutionQueueService, CancelExecutionQueueService>();
        services.AddSingleton<ISqsClient, SqsClient>();
        return services;
    }
}
