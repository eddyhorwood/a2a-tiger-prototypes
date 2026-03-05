using FluentAssertions;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using PaymentExecution.Common;

namespace PaymentExecution.FeatureFlagClient.UnitTests;

public class MockFeatureSourceTests
{
    private readonly TestData _testData = TestData.DataSource();

    [Fact]
    public void GivenTestDataFlagValue_WhenMockFeatureServiceReset_ThenFlagHasLocalDevFlagValue()
    {
        // Arrange
        var mockFeatureSource = new MockFeatureSource(_testData);
        var client = GetLdClient(mockFeatureSource.GetDataSource());

        // Act
        mockFeatureSource.ResetToDefaultLocalDevValues();

        // Assert
        var flagValue = client.BoolVariation(
            ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled.Name, Context.New("default"), false);
        flagValue.Should().BeTrue();
    }

    private static LdClient GetLdClient(TestData datasource)
    {
        var config = Configuration.Builder("some_key").DataSource(datasource).Build();
        var client = new LdClient(config);
        return client;
    }
}
