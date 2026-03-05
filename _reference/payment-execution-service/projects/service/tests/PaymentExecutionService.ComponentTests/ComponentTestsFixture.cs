using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server.Integrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;
using PaymentExecutionService.ComponentTests.Auth;
using WireMock.Server;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Identity.Authentication.HealthChecks.Testing;

namespace PaymentExecutionService.ComponentTests;

public class ComponentTestsFixture : WebApplicationFactory<Program>, IDisposable
{
    public const string NonWhitelistedClientId = "local_caller_non-whitelisted";
    public static readonly JsonSerializerOptions DefaultJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IdentityClient _identityClient;

    public WireMockServer WireMockServer { get; internal set; }

    public ComponentTestsFixture()
    {
        _identityClient = new IdentityClient();
        WireMockServer = WireMockServer.Start();
    }

    public IPaymentTransactionRepository GetPaymentTransactionRepository()
    {
        using var scope = Server.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
    }

    public IPaymentTransactionComponentTestRepository GetTestingPaymentTransactionRepositoryInterface()
    {
        using var scope = Server.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPaymentTransactionComponentTestRepository>();
    }

    public TestData GetServerFeatureServiceDataSource()
    {
        return Server.Services.GetRequiredService<IMockFeatureSource>().GetDataSource();
    }

    public SqsUtility SqsUtility => Server.Services.GetRequiredService<SqsUtility>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Custom config
            // This will automatically return healthy for all configured Identity authorities
            services.AddScoped<IPaymentTransactionComponentTestRepository, PaymentTransactionComponentTestRepository>();
            services.AddXeroIdentityHealthChecksTesting();
            services.AddSingleton<SqsUtility>();
        });
    }

    public HttpClient CreateAuthenticatedClient(string? requiredScope = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _identityClient.GetAccessTokenForHeaderRetrieval(requiredScope));
        return client;
    }

    public HttpClient CreateAuthenticatedClientWithTenantId(string? requiredScope = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _identityClient.GetAccessTokenForHeaderRetrieval(requiredScope));
        client.DefaultRequestHeaders.Add("Xero-Tenant-Id", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateAuthenticatedClientWithClientId(string clientId, string? requiredScope = null)
    {
        var client = CreateClient();
        var identityClient = new IdentityClient(clientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identityClient.GetAccessTokenForHeaderRetrieval(requiredScope));
        return client;
    }

    public async Task<Guid> InsertPaymentTransactionIfNotExist(InsertPaymentTransactionDto paymentTransactionDto)
    {
        var repository = GetPaymentTransactionRepository();
        var result = await repository.InsertPaymentTransactionIfNotExist(paymentTransactionDto);
        return result.Value;
    }

    public async Task WipePaymentTransactionDb()
    {
        using var scope = Server.Services.CreateScope();
        var testRepository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionComponentTestRepository>();
        await testRepository.WipeDb();
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient().WithXeroCorrelationId();
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected new void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        WireMockServer.Stop();
        WireMockServer.Dispose();
    }
}
