using dotenv.net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.StripeExecutionClient;
using PaymentExecutionService.ProviderPactTests.Mocks;
using PaymentExecutionService.Registrations;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xero.Accelerators.Api.Core.Observability.Correlation;
using static Xero.Accelerators.Api.Core.Constants;

namespace PaymentExecutionService.ProviderPactTests.Config;

public class ProviderTestsStartup
{
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // This order matters!
        // We add the provider state middleware here to ensure that provider state endpoint is processed
        // before other requirements like correlation, tenant ids. 
        app.UseMiddleware<ProviderStateMiddleware>();
        app.UseInboundXeroCorrelationIdMiddleware();

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(e => e.MapControllers());
        app.UseHeaderPropagation();

        DotEnv.Load(new DotEnvOptions(probeForEnv: true));
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Constants.CatalogueMetadata);

        services.AddSingleton<IPolicyEvaluator, MockPolicyEvaluator>();
        services.AddSingleton<IAuthorizationPolicyProvider, MockPolicyProvider>();

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appsettings.json", false, true)
            .AddJsonFile("appsettings.Development.json", false, true)
            .AddEnvironmentVariables(prefix: "Override_")
            .Build();

        services.AddServices(configuration);

        // application level mocks 
        services.AddSingleton<IPaymentRequestClient, MockPaymentRequestClient>();
        services.AddSingleton<IStripeExecutionInternalHttpClient, MockStripeExecutionInternalHttpClient>();
        services.AddSingleton(TimeProvider.System);

        // Do we need to setup or tear down any test resources? Maybe not. 
        // services.AddScoped<IPaymentExecutionTestUtilityRepo, PaymentExecutionTestUtilityRepo>();

        services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        });
        services.AddControllers(opt =>
            {
                // Maybe just do not do the following filter so we don't need to mock LD?
                // opt.Filters.Add<ServiceFeatureFlagFilter>();
                opt.Conventions.Add(new SlugifyParameterTransformerConvention());
            })
            .AddApplicationPart(typeof(Program).Assembly);
        services.AddHeaderPropagation(opt =>
        {
            opt.Headers.Add(HttpHeaders.XeroCorrelationId);
            opt.Headers.Add(HttpHeaders.XeroTenantId);
            opt.Headers.Add(HttpHeaders.XeroUserId);
            opt.Headers.Add("Content-Type", "application/json");
        });
    }
}
