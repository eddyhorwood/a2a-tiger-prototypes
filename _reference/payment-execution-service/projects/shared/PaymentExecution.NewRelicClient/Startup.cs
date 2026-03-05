using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentExecution.NewRelicClient;

[ExcludeFromCodeCoverage]
public static class Startup
{
    public static IServiceCollection AddNewRelicClient(this IServiceCollection services)
    {
        services.AddSingleton<IMonitoringClient, NewRelicClient>();
        return services;
    }
}
