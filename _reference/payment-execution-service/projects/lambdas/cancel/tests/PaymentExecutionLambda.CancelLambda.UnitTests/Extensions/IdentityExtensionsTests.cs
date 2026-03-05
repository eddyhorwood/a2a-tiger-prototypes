using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecutionLambda.CancelLambda.Extensions;

namespace PaymentExecutionLambda.CancelLambda.UnitTests.Extensions;

public class IdentityExtensionsTests
{
    private readonly ServiceCollection _services = [];

    [Fact]
    public void GivenValidConfiguration_WhenConfigureXeroIdentityClient_ThenRegistersIdentityClient()
    {
        // Arrange
        var configuration = BuildConfiguration(
            authority: "https://identity.xero.com",
            clientId: "test-client-id",
            clientSecret: "test-secret",
            scopes: new[] { "test.scope" }
        );

        // Act
        _services.ConfigureXeroIdentityClient(configuration);

        // Assert - verify service collection has been configured
        _services.Should().NotBeEmpty();
    }


    private static IConfiguration BuildConfiguration(
        string authority,
        string clientId,
        string clientSecret,
        string[] scopes)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Identity:Authority"] = authority,
            ["Identity:Client:ClientId"] = clientId,
            ["Identity:Client:ClientSecret"] = clientSecret,
        };

        for (var index = 0; index < scopes.Length; index++)
        {
            configValues[$"Identity:Client:ClientScopes:{index}"] = scopes[index];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }
}

