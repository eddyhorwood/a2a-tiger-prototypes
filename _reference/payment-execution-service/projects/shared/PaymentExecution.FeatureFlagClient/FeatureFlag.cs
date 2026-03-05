namespace PaymentExecution.FeatureFlagClient;

public class FeatureFlag<T>
{
    public required string Name { get; set; } = string.Empty;
    public required T Value { get; set; }
}
