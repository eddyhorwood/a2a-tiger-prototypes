namespace PaymentExecution.FeatureFlagClient;

public class LaunchDarklyOptions
{
    public const string Key = "LaunchDarkly";
    public required string SdkKey { get; set; }
}
