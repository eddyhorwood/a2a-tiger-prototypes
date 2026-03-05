
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PactNet;
using PactNet.Verifier;
using PaymentExecutionService.ProviderPactTests.Config;
using PaymentExecutionService.Registrations;
using Xunit.Abstractions;

namespace PaymentExecutionService.ProviderPactTests;

public class ProviderTests : IDisposable
{
    private readonly PactVerifier _verifier;
    private readonly IHost _server;
    private const string ProviderUri = "http://localhost:9001";

    public ProviderTests(ITestOutputHelper output)
    {
        _server = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(opts =>
            {
                opts.ValidateOnBuild = false;
                opts.ValidateScopes = false;
            })
            .ConfigureWebHostDefaults(builder =>
            {
                builder.AddPactCompatibleServiceVersioning();
                builder.UseUrls(ProviderUri);
                builder.UseStartup<ProviderTestsStartup>();
            })
            .Build();

        _server.Start();
        _verifier = new PactVerifier("PaymentExecutionService",
            new PactVerifierConfig { LogLevel = PactLogLevel.Debug, Outputters = [new XUnitOutput(output)] });
    }

    public void Dispose()
    {
        _server.Dispose();
        _verifier.Dispose();
        GC.SuppressFinalize(this);
    }

    /***
     * This test pulls in consumer contracts from
     * 1. A local json pact file path if PACT_FILE_PATH is set
     * 2. A specific pact json file if PACT_URL is set
     * 3. The PactFlow broker, reading contracts for the consumers listed in SupportedConsumers.cs if neither of those
     *    variables are set.
     *
     * It then verifies those contracts against this service.
     * Only if the CI env variable is set to true, will it publish the verification results.
     * Read more here https://accelerators.xero.dev/docs/api-accelerator/guides/pact-provider-tests/
     */
    [Fact]
    public void ProviderVerifications()
    {
        // Arrange
        var pactFlowToken = GetPactEnvironmentVariable("PACT_BROKER_TOKEN");
        var pactFlowBaseUrl = GetPactEnvironmentVariable("PACT_BROKER_BASE_URL");
        var branch = GetPactEnvironmentVariable("PACT_GIT_BRANCH");
        var version = GetPactEnvironmentVariable("PACT_PROVIDER_VERSION");
        var buildUri = GetPactEnvironmentVariable("PACT_BUILD_URI");

        var pactFilePath = Environment.GetEnvironmentVariable($"PACT_FILE_PATH");
        var pactUrl = Environment.GetEnvironmentVariable("PACT_URL");
        var isCi = Environment.GetEnvironmentVariable("CI") == "true";

        var concreteVerifier = _verifier.WithHttpEndpoint(new Uri(ProviderUri));
        var finalVerifier =
            pactFilePath != null ? SetupPactVerifierWithSpecificFileSource(concreteVerifier, new FileInfo(pactFilePath)) :
            pactUrl != null ? SetupPactVerifierWithSpecificUriSource(concreteVerifier, pactUrl, pactFlowToken, isCi, version, branch, buildUri) :
            SetupVerifiedWithGeneralPactBrokerSource(concreteVerifier, pactFlowBaseUrl, pactFlowToken, isCi, version, branch, buildUri);

        // Act + Assert
        finalVerifier.WithRequestTimeout(TimeSpan.FromMinutes(20))
            .Verify();
    }

    private static IPactVerifierSource SetupVerifiedWithGeneralPactBrokerSource(IPactVerifier concreteVerifier, string pactFlowBaseUrl, string pactFlowToken, bool isCi, string version, string branch, string buildUri)
    {
        return concreteVerifier
            .WithPactBrokerSource(new Uri(pactFlowBaseUrl), options =>
            {
                // Allow consumers to add new pacts to the broker without provider test immediately failing until at least one successful verification has been run
                options.EnablePending();
                options.IncludeWipPactsSince(DateTime.Parse("2025-04-03"));

                options.ConsumerVersionSelectors(CreateConsumerVersionSelectors());
                options.TokenAuthentication(pactFlowToken);
                options.PublishResults(isCi, version, publishOptions =>
                {
                    publishOptions.ProviderBranch(branch);
                    publishOptions.BuildUri(new Uri(buildUri));
                });
            });
    }

    private static List<ConsumerVersionSelector> CreateConsumerVersionSelectors()
    {
        List<ConsumerVersionSelector> versionSelectorList = [];
        SupportedConsumers.GetPactConsumerNames().ForEach(consumerName =>
        {
            versionSelectorList.Add(
                new ConsumerVersionSelector { DeployedOrReleased = true, Consumer = consumerName });
            versionSelectorList.Add(
                new ConsumerVersionSelector { MainBranch = true, Consumer = consumerName });
        });
        return versionSelectorList;
    }

    private static IPactVerifierSource SetupPactVerifierWithSpecificUriSource(IPactVerifier concreteVerifier, string pactUrl, string pactFlowToken, bool isCi, string version, string branch, string buildUri)
    {
        return concreteVerifier.WithUriSource(new Uri(pactUrl), options =>
        {
            options.TokenAuthentication(pactFlowToken);
            options.PublishResults(isCi, version, publishOptions =>
            {
                publishOptions.ProviderBranch(branch);
                publishOptions.BuildUri(new Uri(buildUri));
            });
        });
    }

    private static IPactVerifierSource SetupPactVerifierWithSpecificFileSource(IPactVerifier concreteVerifier, FileInfo pactFile)
    {
        return concreteVerifier.WithFileSource(pactFile);
    }

    private static string GetPactEnvironmentVariable(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? throw new Exception($"{variableName} must be provided");
    }
}
