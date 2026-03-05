using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentExecution.FeatureFlagClient;

public static class LaunchDarklyClientStartup
{
    public static IServiceCollection AddLaunchDarkly(this IServiceCollection services, IConfiguration configuration)
    {
        var ldOptionsSection = configuration.GetSection(LaunchDarklyOptions.Key);
        services.Configure<LaunchDarklyOptions>(ldOptionsSection);
        var ldOptions = ldOptionsSection.Get<LaunchDarklyOptions>() ??
                        throw new ArgumentException(
                            "LaunchDarkly configuration section not provided or missing required fields");
        var isDevEnvironment = "Development".Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));

        if (!isDevEnvironment &&
            string.IsNullOrWhiteSpace(ldOptions.SdkKey))
        {
            throw new ArgumentException("LaunchDarkly SDK Key Missing");
        }

        if (isDevEnvironment)
        {
            var td = TestData.DataSource();
            var mockFeatureSource = new MockFeatureSource(td);
            services.AddSingleton<IMockFeatureSource, MockFeatureSource>(_ => mockFeatureSource);
            services.AddSingleton<ILdClient>((_) => CreateInMemoryTestDataSourceLdClient(ldOptions, td));
        }
        else
        {
            services.AddSingleton<ILdClient>(_ => new LdClient(ldOptions.SdkKey));
        }

        services.AddSingleton<IFeatureFlagClient, LaunchDarklyClient>();
        return services;
    }

    private static LdClient CreateInMemoryTestDataSourceLdClient(LaunchDarklyOptions ldOptions, TestData td)
    {
        var launchDarklyConfig = Configuration
            .Builder(ldOptions.SdkKey)
            .DataSource(
                td)
            .Build();
        return new LdClient(launchDarklyConfig);
    }
}
