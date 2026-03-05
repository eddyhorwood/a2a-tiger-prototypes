using System.Reflection;
using Amazon.Lambda.Annotations;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FluentAssertions;
using LaunchDarkly.Sdk.Server.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.StripeExecutionClient.Contracts;
using PaymentExecutionLambda.CancelLambda;

namespace PaymentExecutionLambda.CancelLambdaUnitTests;

public sealed class StartupTests
{
    private readonly ServiceCollection _services = [];

    [Fact]
    public void GivenStartupClass_WhenCheckingForAttribute_ThenShouldHaveLambdaStartupAttribute()
    {
        // Arrange & Act
        var attribute = typeof(Startup).GetCustomAttribute<LambdaStartupAttribute>();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void GivenConfigureServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var startup = new Startup();
        SetupTestEnvironmentVariables();

        // Act
        startup.ConfigureServices(_services);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<ILogger<Startup>>().Should().NotBeNull();
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<IFeatureFlagClient>().Should().NotBeNull();
        serviceProvider.GetService<ILdClient>().Should().NotBeNull();
        serviceProvider.GetService<IStripeExecutionClient>().Should().NotBeNull();
        serviceProvider.GetService<IHttpClientFactory>().Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldSetupConfiguration_WithBasePath()
    {
        // Arrange
        var startup = new Startup();
        SetupTestEnvironmentVariables();

        // Act
        startup.ConfigureServices(_services);
        var serviceProvider = _services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();

        // Assert
        configuration.Should().NotBeNull();
        configuration.Should().BeAssignableTo<IConfiguration>();
    }

    [Fact]
    public void ConfigureServices_ShouldUseCurrentDirectory_WhenLambdaTaskRootNotSet()
    {
        // Arrange
        var startup = new Startup();
        SetupTestEnvironmentVariables();
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", null); // Override to test null case

        // Act
        startup.ConfigureServices(_services);
        var serviceProvider = _services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();

        // Assert
        configuration.Should().NotBeNull();
        configuration.Should().BeAssignableTo<IConfiguration>();
    }

    [Fact]
    public void GetEnvironmentName_ShouldReturnEnvironmentVariable_WhenSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENVIRONMENT", "production");

        // Act
        var result = Startup.GetEnvironmentName();

        // Assert
        result.Should().Be("production");

        // Cleanup
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);
    }

    [Fact]
    public void GetEnvironmentName_ShouldReturnDevelopment_WhenNotSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);

        // Act
        var result = Startup.GetEnvironmentName();

        // Assert
        result.Should().Be("Development");
    }

    [Fact]
    public void GetEnvironmentName_ShouldReturnDevelopment_WhenEmptyString()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENVIRONMENT", string.Empty);

        // Act
        var result = Startup.GetEnvironmentName();

        // Assert
        result.Should().Be("Development");

        // Cleanup
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);
    }

    [Fact]
    public void GetSecretValueFromSecretsManager_ShouldReturnSecretString_WhenValid()
    {
        // Arrange
        var mockClient = new Mock<IAmazonSecretsManager>();
        const string ExpectedSecret = "my-secret-value";
        const string SecretId = "my-secret-id";
        const string EnvVarName = "MY_ENV_VAR";

        mockClient
            .Setup(x => x.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == SecretId), default))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = ExpectedSecret });

        // Act
        var result = Startup.GetSecretValueFromSecretsManager(mockClient.Object, EnvVarName, SecretId);

        // Assert
        result.Should().Be(ExpectedSecret);
        mockClient.Verify(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Once);
    }

    [Fact]
    public void GetSecretValueFromSecretsManager_ShouldThrowInvalidOperationException_WhenSecretStringIsNull()
    {
        // Arrange
        var mockClient = new Mock<IAmazonSecretsManager>();
        const string SecretId = "my-secret-id";
        const string EnvVarName = "MY_ENV_VAR";

        mockClient
            .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = null });

        // Act
        var act = () => Startup.GetSecretValueFromSecretsManager(mockClient.Object, EnvVarName, SecretId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Failed to retrieve secret from Secrets Manager*{EnvVarName}*{SecretId}*")
            .WithInnerException<InvalidOperationException>()
            .WithMessage($"*Retrieved secret value is null or empty*{EnvVarName}*{SecretId}*");
    }

    [Fact]
    public void GetSecretValueFromSecretsManager_ShouldThrowInvalidOperationException_WhenSecretStringIsEmpty()
    {
        // Arrange
        var mockClient = new Mock<IAmazonSecretsManager>();
        const string SecretId = "my-secret-id";
        const string EnvVarName = "MY_ENV_VAR";

        mockClient
            .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = string.Empty });

        // Act
        var act = () => Startup.GetSecretValueFromSecretsManager(mockClient.Object, EnvVarName, SecretId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Failed to retrieve secret from Secrets Manager*{EnvVarName}*{SecretId}*")
            .WithInnerException<InvalidOperationException>()
            .WithMessage($"*Retrieved secret value is null or empty*{EnvVarName}*{SecretId}*");
    }

    [Fact]
    public void GetSecretValueFromSecretsManager_ShouldThrowInvalidOperationException_WhenSecretStringIsWhitespace()
    {
        // Arrange
        var mockClient = new Mock<IAmazonSecretsManager>();
        const string SecretId = "my-secret-id";
        const string EnvVarName = "MY_ENV_VAR";

        mockClient
            .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default))
            .ReturnsAsync(new GetSecretValueResponse { SecretString = "   " });

        // Act
        var act = () => Startup.GetSecretValueFromSecretsManager(mockClient.Object, EnvVarName, SecretId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Failed to retrieve secret from Secrets Manager*{EnvVarName}*{SecretId}*")
            .WithInnerException<InvalidOperationException>()
            .WithMessage($"*Retrieved secret value is null or empty*{EnvVarName}*{SecretId}*");
    }

    [Fact]
    public void GetSecretValueFromSecretsManager_ShouldThrowInvalidOperationException_WhenSecretsManagerThrowsException()
    {
        // Arrange
        var mockClient = new Mock<IAmazonSecretsManager>();
        const string SecretId = "my-secret-id";
        const string EnvVarName = "MY_ENV_VAR";

        mockClient
            .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default))
            .ThrowsAsync(new ResourceNotFoundException("Secret not found"));

        // Act
        var act = () => Startup.GetSecretValueFromSecretsManager(mockClient.Object, EnvVarName, SecretId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Failed to retrieve secret from Secrets Manager*{EnvVarName}*{SecretId}*")
            .WithInnerException<ResourceNotFoundException>();
    }

    [Fact]
    public void ResolveSecretsManagerSecrets_ShouldDoNothing_WhenEnvironmentVariableNamesIsNull()
    {
        // Act
        var act = () => Startup.ResolveSecretsManagerSecrets(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ResolveSecretsManagerSecrets_ShouldDoNothing_WhenEnvironmentVariableNamesIsEmpty()
    {
        // Act
        var act = () => Startup.ResolveSecretsManagerSecrets(Array.Empty<string>());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GivenConfigureServices_WhenStripeExecutionClientConfigured_ThenRequiredServicesAreRegistered()
    {
        // Arrange
        var startup = new Startup();
        SetupTestEnvironmentVariables();

        // Act
        startup.ConfigureServices(_services);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Verify StripeExecutionClient is available (which requires Identity client to be configured)
        var stripeExecutionClient = serviceProvider.GetService<IStripeExecutionClient>();
        stripeExecutionClient.Should().NotBeNull("StripeExecutionClient should be registered to make authenticated calls to Stripe");

        // Verify HttpClient factory is available (used by StripeExecutionInternalHttpClient with IdentityTokenRefreshingHandler)
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull("HttpClientFactory should be registered for StripeExecutionInternalHttpClient which uses Identity authentication");
    }

    [Fact]
    public void GivenConfigureServices_WhenIdentityClientConfigured_ThenHttpClientFactoryIsAvailable()
    {
        // Arrange
        var startup = new Startup();
        SetupTestEnvironmentVariables();

        // Act
        startup.ConfigureServices(_services);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Verify that HttpClient factory is available (which is used by StripeExecutionInternalHttpClient with IdentityTokenRefreshingHandler)
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull("HttpClientFactory should be registered to support Identity token handling in HTTP requests");

        // Verify we can create an HttpClient (this indirectly validates Identity configuration since StripeExecutionInternalHttpClient depends on it)
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Should().NotBeNull("HttpClient should be created successfully with all configured handlers including IdentityTokenRefreshingHandler");
    }


    private static void SetupTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("LAMBDA_TASK_ROOT", Directory.GetCurrentDirectory());
    }
}
