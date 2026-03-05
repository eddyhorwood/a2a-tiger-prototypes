using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;

namespace PaymentExecution.FeatureFlagClient;

public interface IFeatureFlagClient
{
    FeatureFlag<bool> GetFeatureFlag(FeatureFlagDefinition<bool> flagDefinition, Context? context = null);
    bool Initialized { get; }
}

public class LaunchDarklyClient(ILogger<LaunchDarklyClient> logger, ILdClient ldClient) : IFeatureFlagClient
{
    public FeatureFlag<bool> GetFeatureFlag(FeatureFlagDefinition<bool> flagDefinition, Context? context = null)
    {
        var flagValue = ldClient.BoolVariation(flagDefinition.Name, context ?? Context.New("default"),
            flagDefinition.DefaultValue);
        logger.LogInformation("Retrieved feature flag {FlagName} with value {FlagValue}", flagDefinition.Name,
            flagValue);
        return new FeatureFlag<bool>() { Name = flagDefinition.Name, Value = flagValue };
    }

    public bool Initialized { get { return ldClient.Initialized; } }
}
