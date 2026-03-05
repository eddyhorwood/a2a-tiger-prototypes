using System.Collections.Generic;
using System.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PaymentExecutionService.Models;
using Xunit;

namespace PaymentExecutionService.UnitTests.Models;

public class WhitelistOptionsTests
{
    [Fact]
    public void GivenValidWhitelistConfiguration_WhenAddServiceWhitelistCalled_ThenRegistersWhitelistOptions()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Whitelist:ClientIds:0", "client1" },
                { "Whitelist:ClientIds:1", "client2" }
            })
            .Build();
        builder.Configuration.AddConfiguration(configuration);
        var expectedWhitelistOptions = new WhitelistOptions
        {
            ClientIds = new List<string> { "client1", "client2" }
        };

        // Act
        builder.Services.AddServiceWhitelistOptions(builder.Configuration);
        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<WhitelistOptions>>().Value;

        // Assert
        Assert.NotNull(options);
        Assert.Equivalent(expectedWhitelistOptions, options);
    }

    [Fact]
    public void GivenNoWhitelistConfiguration_WhenAddServiceWhitelistCalled_ThenThrowsExceptionOnRegistration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        // Act
        var act = () => builder.Services.AddServiceWhitelistOptions(builder.Configuration);

        // Assert
        Assert.Throws<ConfigurationErrorsException>(act);
    }

    [Fact]
    public void GivenClientIdsIsNullInWhitelistConfiguration_WhenAddServiceWhitelistCalled_ThenThrowsExceptionOnRegistration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Whitelist:ClientIds", null }
            })
            .Build();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        var act = () => builder.Services.AddServiceWhitelistOptions(builder.Configuration);

        // Assert
        Assert.Throws<ConfigurationErrorsException>(act);
    }

    [Fact]
    public void GivenEmptyClientIdsInWhitelistConfiguration_WhenAddServiceWhitelistCalled_ThenThrowsExceptionOnRegistration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Whitelist:ClientIds:0", "" }
            })
            .Build();
        builder.Configuration.AddConfiguration(configuration);

        // Act
        var act = () => builder.Services.AddServiceWhitelistOptions(builder.Configuration);

        // Assert
        Assert.Throws<ConfigurationErrorsException>(act);
    }
}
