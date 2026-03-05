using System.Reflection;
using Asp.Versioning;
using Npgsql;
using PaymentExecution.NewRelicClient;
using PaymentExecutionService.Controllers;
using Polly;
using Polly.CircuitBreaker;

namespace PaymentExecutionService.Middleware.ActionCircuitBreakers;

public static class CircuitBreakerMiddlewareExtensions
{
    private const string BaseActionCircuitBreakerConfigPath = "ResilienceConfigurations:EndpointCircuits:";

    public static IServiceCollection RegisterEndpointCircuitPipelines(this IServiceCollection services,
        IConfiguration configuration)
    {
        CreateEndpointActionCircuits(services, configuration);

        return services;
    }

    private static void CreateEndpointActionCircuits(IServiceCollection services, IConfiguration configuration)
    {
        var asm = Assembly.GetExecutingAssembly();
        var controllerTypes = asm.GetTypes()
            .Where(type => typeof(BaseController).IsAssignableFrom(type));
        var circuitsPaths = controllerTypes.SelectMany(controller =>
        {
            var apiVersionAttribute = controller.GetCustomAttribute<ApiVersionAttribute>();
            return apiVersionAttribute?.Versions
                       .SelectMany(version => ExtractEndpointActionPathsFromControllerAtVersion(controller, version))
                   ?? ExtractEndpointActionPathsFromControllerAtVersion(controller, new ApiVersion(1, 0));
        });


        foreach (var key in circuitsPaths)
        {
            services.AddEndpointCircuitBreakerPipeline(configuration, key);
        }
    }

    private static IEnumerable<string> ExtractEndpointActionPathsFromControllerAtVersion(Type controller, ApiVersion version)
    {
        return controller
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(UseCircuitBreakerAttribute), false).Length > 0)
            .Select(method => $"v{version.MajorVersion}.{version.MinorVersion ?? 0}:{controller.Name}:{method.Name}");
    }

    public static IServiceCollection AddEndpointCircuitBreakerPipeline(this IServiceCollection services,
        IConfiguration configuration,
        string configurationPath)
    {
        var circuitSection = configuration.GetSection(BaseActionCircuitBreakerConfigPath + configurationPath);
        var circuitOpts = circuitSection.Get<EndpointCircuitBreakerPipelineOptions>();

        services.AddResiliencePipeline(configurationPath,
            (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<CircuitMiddleware>>();
                var newRelicClient = context.ServiceProvider.GetRequiredService<IMonitoringClient>();
                builder.AddCircuitBreaker(
                    new CircuitBreakerStrategyOptions()
                    {
                        FailureRatio = circuitOpts?.FailureRatio ?? 0.1,
                        SamplingDuration = TimeSpan.FromSeconds(circuitOpts?.SamplingDurationSeconds ?? 1),
                        MinimumThroughput = circuitOpts?.MinimumThroughput ?? 5,
                        BreakDuration = TimeSpan.FromSeconds(circuitOpts?.BreakDurationSeconds ?? 30),
                        OnOpened = args => OnOpened(args, logger, newRelicClient, configurationPath),
                        OnClosed = _ => OnClosed(logger, newRelicClient, configurationPath),
                        OnHalfOpened = _ => OnHalfOpened(logger, newRelicClient, configurationPath),
                        ShouldHandle = new PredicateBuilder()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutException>()
                            .Handle<NpgsqlException>(ex => ex.IsTransient)
                    });
            });
        return services;
    }

    private static ValueTask OnOpened(OnCircuitOpenedArguments<object> onCircuitOpenedArguments,
        ILogger<CircuitMiddleware> logger, IMonitoringClient monitoringClient, string circuitPath)
    {
        logger.LogError(
            "Circuit '{CircuitPath}' opened. Break duration: {Duration}", circuitPath, onCircuitOpenedArguments.BreakDuration.ToString());
        monitoringClient.NotifyNewRelicOfCircuitEvent(CircuitState.Open, circuitPath, onCircuitOpenedArguments.BreakDuration);
        return new ValueTask(Task.CompletedTask);
    }

    private static ValueTask OnClosed(
        ILogger<CircuitMiddleware> logger, IMonitoringClient monitoringClient, string circuitPath)
    {
        logger.LogInformation(
            "Circuit '{CircuitPath}' closed.", circuitPath);
        monitoringClient.NotifyNewRelicOfCircuitEvent(CircuitState.Closed, circuitPath);
        return new ValueTask(Task.CompletedTask);
    }

    private static ValueTask OnHalfOpened(
        ILogger<CircuitMiddleware> logger, IMonitoringClient monitoringClient, string circuitPath)
    {
        logger.LogInformation(
            "Circuit '{CircuitPath}' half-opened for probe query.", circuitPath);
        monitoringClient.NotifyNewRelicOfCircuitEvent(CircuitState.HalfOpen, circuitPath);
        return new ValueTask(Task.CompletedTask);
    }
}
