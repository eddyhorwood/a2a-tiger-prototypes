using FluentAssertions;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;

namespace PaymentExecution.FeatureFlagClient.UnitTests;

public class LaunchDarklyClientTests
{
    private readonly FeatureFlagDefinition<bool> FlagDefinition = new FeatureFlagDefinition<bool>("some-key", false);

    private readonly Mock<ILogger<LaunchDarklyClient>> _mockLogger = new();
    private readonly TestData _testDatasource = TestData.DataSource();
    private readonly LaunchDarklyClient _sut;

    public LaunchDarklyClientTests()
    {
        _sut = new LaunchDarklyClient(_mockLogger.Object, new LdClient(
            Configuration.Builder("some-key")
                .DataSource(_testDatasource).Build())
        );
    }

    [Fact]
    public void GivenBoolFeaturePresentInLd_WhenRequestMadeForFeatureValue_FeatureIsReturned()
    {
        // Arrange

        _testDatasource.Update(_testDatasource.Flag(FlagDefinition.Name).VariationForAll(true));
        var expectedFeatureFlag = new FeatureFlag<bool>() { Name = FlagDefinition.Name, Value = true };

        // Act
        var result = _sut.GetFeatureFlag(FlagDefinition);
        result.Should().BeEquivalentTo(expectedFeatureFlag);
    }

    [Fact]
    public void GivenIntFeaturePresentInLd_WhenRequestMadeForBoolFeatureValue_TrueValueIsReturnedAsFlagPresent()
    {
        // Arrange
        _testDatasource.Update(_testDatasource.Flag(FlagDefinition.Name).VariationForAll(0));
        var expectedFeatureFlag = new FeatureFlag<bool>() { Name = FlagDefinition.Name, Value = true };

        // Act
        var result = _sut.GetFeatureFlag(FlagDefinition);

        // Assert
        result.Should().BeEquivalentTo(expectedFeatureFlag);
    }


    [Fact]
    public void GivenBoolFeatureNotPresentInLd_WhenRequestMadeForFeatureValue_DefaultValueIsReturned()
    {
        // Arrange
        _testDatasource.Update(_testDatasource.Flag(FlagDefinition.Name).VariationForAll(false));
        var expectedFeatureFlag = new FeatureFlag<bool>() { Name = "some random key not in test flags", Value = true };

        // Act
        var result = _sut.GetFeatureFlag(new FeatureFlagDefinition<bool>("some random key not in test flags", true));

        // Assert
        result.Should().BeEquivalentTo(expectedFeatureFlag);
    }

    [Fact]
    public void GivenInitialized_WhenInitialzed_ThenReturnClientInitializedState()
    {
        // Arrange
        _testDatasource.Update(_testDatasource.Flag(FlagDefinition.Name).VariationForAll(false));

        // Act
        var result = _sut.Initialized;

        // Assert
        result.Should().BeTrue();
    }
}
