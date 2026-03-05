using FluentAssertions;
using LaunchDarkly.Sdk.Server.Integrations;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace PaymentExecution.FeatureFlagClient.UnitTests;

public class StartupTests
{
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly ServiceCollection _services = [];


    public StartupTests()
    {
        _configurationMock.Setup(c =>
            c.GetSection(It.IsAny<string>())).Returns(GetLaunchDarklyConfigSection());
    }

    private static IConfigurationSection GetLaunchDarklyConfigSection(string sdkKeyValue = "your-sdk-key") => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { "LaunchDarkly:SdkKey", sdkKeyValue },
        }!)
        .Build()
        .GetSection("LaunchDarkly");

    [Fact]
    public void GivenStartupWithLocalDevModeEnabled_WhenStartupIsConstructed_ThenMockFeatureSourceIsAvailable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        // Act
        _services.AddLaunchDarkly(_configurationMock.Object);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var mockFeatureSource = serviceProvider.GetService<IMockFeatureSource>();
        mockFeatureSource!.GetDataSource().GetType().Should().Be(typeof(TestData));
        Assert.NotNull(serviceProvider.GetService<IMockFeatureSource>());
        Assert.NotNull(serviceProvider.GetService<ILdClient>());
    }

    [Fact]
    public void
        GivenStartupWithUseMocksOnButInNonDevelopmentEnv_WhenStartupIsConstructed_ThenMockFeatureSourceIsNotAvailable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");

        // Act
        _services.AddLaunchDarkly(_configurationMock.Object);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.Null(serviceProvider.GetService<IMockFeatureSource>());
        Assert.NotNull(serviceProvider.GetService<ILdClient>());
    }

    [Fact]
    public void GivenInNonDevEnvironmentAndWhitespaceSdkKey_WhenStartupIsConstructed_ThenArgumentExceptionShouldBeThrown()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");
        _configurationMock.Setup(c =>
            c.GetSection(It.IsAny<string>())).Returns(GetLaunchDarklyConfigSection(""));

        // Act + Assert
        Assert.Throws<ArgumentException>(() => _services.AddLaunchDarkly(_configurationMock.Object));
    }

}
