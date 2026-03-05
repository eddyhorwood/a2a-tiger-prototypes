using System.IO.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecution.Domain;
using PaymentExecution.Domain.Service;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.NewRelicClient;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.Repository;
using PaymentExecution.SqsIntegrationClient;
using PaymentExecution.StripeExecutionClient;
using PaymentExecutionWorker.Worker;
using PaymentExecutionWorker.Worker.HealthCheck;
using PaymentExecutionWorker.XeroIdentity;
using Serilog;

const string ClientName = "Payment-Execution-Worker";

var cancellationToken = new CancellationToken();

// Partially Satisfies XREQ-44, XREQ-45
var host = CreateHostBuilder(args)
    .Build();

await host.RunAsync(cancellationToken);
await host.StopAsync(cancellationToken);
return 0;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        // Satisfies XREQ-291
        .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .Enrich.FromLogContext()
        )
        .ConfigureAppConfiguration(configuration => configuration.AddEnvironmentVariables(prefix: "Override_"))

        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<ServiceProviderOptions>(opts =>
            {
                opts.ValidateScopes = true;
            });
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddHealthChecks()
                .AddCheck<HealthCheck>("live", tags: ["live"])
                .AddCheck<DbHealthCheck>("db_health_check", tags: ["dbready"]);
            services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(3);
                options.Period = TimeSpan.FromSeconds(10);
            });
            services.AddNewRelicClient();
            services.AddLaunchDarkly(hostContext.Configuration);
            services.AddScoped<IProcessCompleteMessageDomainService, ProcessCompleteMessageDomainService>();
            services.AddSqsIntegration(hostContext.Configuration);
            services.AddDbIntegrationServices(hostContext.Configuration);
            services.AddXeroIdentityRegistration(hostContext.Configuration);
            services.AddPaymentRequestClient(ClientName, hostContext.Configuration);

            //Temporarily added to allow mediator to resolve - to be removed as part of INPAY-20744
            services.AddProviderIntegrationServices();

            //Temporarily added to allow mediator to resolve - ExecutionClient to be removed as part of INPAY-20744
            services.AddScoped<IRequestCancelDomainService, RequestCancelDomainService>();
            services.AddScoped<ICancellationValidationService, CancellationValidationService>();

            services.AddStripeExecutionClient(ClientName, hostContext.Configuration);
            services.AddSingleton(TimeProvider.System);
            services.ConfigMediatR(hostContext.Configuration);
            services.AddHostedService<Worker>();
        });

// Needed for integration tests to reference Program as a type parameter
#pragma warning disable S1118
public partial class Program
{
}
#pragma warning restore S1118
