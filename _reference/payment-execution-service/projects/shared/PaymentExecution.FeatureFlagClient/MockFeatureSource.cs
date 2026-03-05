using LaunchDarkly.Sdk.Server.Integrations;

namespace PaymentExecution.FeatureFlagClient;

public interface IMockFeatureSource
{
    TestData GetDataSource();
    void ResetToDefaultLocalDevValues();
}

public class MockFeatureSource : IMockFeatureSource
{
    private readonly TestData _datasource;
    public MockFeatureSource(TestData datasource)
    {
        _datasource = datasource;
        _datasource.SetLocalDevFlagValues();
    }

    public TestData GetDataSource()
    {
        return _datasource;
    }

    public void ResetToDefaultLocalDevValues()
    {
        _datasource.SetLocalDevFlagValues();
    }
}
