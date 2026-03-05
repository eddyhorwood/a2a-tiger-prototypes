using System;
using System.Net.Http;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;

namespace PaymentExecutionService.ComponentTests.Auth;

public class IdentityClient
{
    private readonly TokenClient _client;
    private readonly IdentityOptions _identityConfig;

    public IdentityClient()
    {
        _identityConfig = GetIdentityConfig();
        var options = new TokenClientOptions
        {
            Address = $"{_identityConfig.Authority}/connect/token",
            ClientId = "xero_identity_test-user-token-client",
            ClientSecret = _identityConfig.Client!.ClientSecret
        };
        _client = new TokenClient(new HttpClient(), options);
    }

    public IdentityClient(string clientId)
    {
        _identityConfig = GetIdentityConfig();
        var options = new TokenClientOptions
        {
            Address = $"{_identityConfig.Authority}/connect/token",
            ClientId = clientId,
            ClientSecret = _identityConfig.Client!.ClientSecret
        };
        _client = new TokenClient(new HttpClient(), options);
    }

    public string GetAccessTokenForClaimsRetrieval()
    {
        return _client.RequestTokenAsync("xo_test_user_token",
            new Parameters
            {
                { "username", "custom-user@test.xero.com" },
                { "password", "password" },
                { "scope", "xero_collecting-payments-execution_payment-execution-service.submit" }
            }).Result.AccessToken;
    }

    public string GetAccessTokenForHeaderRetrieval(string? requiredScope = null)
    {
        return _client.RequestClientCredentialsTokenAsync(requiredScope ?? _identityConfig.Authentication!.RequiredScopes![0]).Result.AccessToken;
    }

    private static IdentityOptions GetIdentityConfig()
    {
        var hostingEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{hostingEnvironment}.json")
            .AddEnvironmentVariables(prefix: "Override_")
            .Build();

        return config.GetSection(IdentityOptions.Key).Get<IdentityOptions>()!;
    }
}
