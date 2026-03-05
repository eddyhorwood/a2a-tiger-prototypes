using LaunchDarkly.Sdk.Server.Integrations;
using PaymentExecution.Common;

namespace PaymentExecution.FeatureFlagClient;

public static class LaunchDarklyDatasourceExtensions
{
    public static void SetLocalDevFlagValues(this TestData td)
    {
        td.Update(td.Flag(ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled.Name)
            .VariationForAll(true));
        td.Update(td.Flag(ExecutionConstants.FeatureFlags.EnableProviderCancellation.Name)
            .VariationForAll(true));
    }
}
