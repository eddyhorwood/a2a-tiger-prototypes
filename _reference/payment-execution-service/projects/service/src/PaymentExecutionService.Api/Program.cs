using Microsoft.Extensions.Primitives;
using PaymentExecutionService;
using PaymentExecutionService.Extensions;
using PaymentExecutionService.Filters;
using PaymentExecutionService.HealthChecks;
using PaymentExecutionService.Middleware;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using PaymentExecutionService.Registrations;
using Serilog;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xero.Accelerators.Api.Core.Conventions.Versioning;
using Xero.Accelerators.Api.Core.Observability.Correlation;
using Xero.Accelerators.Api.Core.Observability.HealthChecks;
using Xero.Accelerators.Api.Core.Observability.Logging;
using Xero.Accelerators.Api.Core.OpenApi;
using Xero.Accelerators.Api.Core.Security.SecretHeader;
using Xero.Accelerators.Api.Core.Security.XeroAuthorisation;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using static Xero.Accelerators.Api.Core.Constants;

// ==================
// Application setup
// ==================
var builder = CreateWebApplicationBuilder(Constants.CatalogueMetadata);
ConfigureBuilder(builder);

var app = builder.Build();
UseAcceleratorCoreServices(app);
ConfigureApplication(app);

app.MapControllers();

app.Run();

// ==================
// Helper functions
// ==================

// Configure services for your API here
void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.AddHttpRequestRegistrations();

    builder.Services.AddServices(builder.Configuration);

    // Configure health checks https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-8.0
    builder.AddServiceHealthChecks();
}

void ConfigureApplication(WebApplication app)
{
    ConfigureHealthChecks(app);

    app.UseMiddleware<ExceptionLoggingMiddleware>();
    app.UseMiddleware<CircuitMiddleware>();
    app.UseEnforceProviderAccountIdAttribute();
    app.UseEnforceTenantIdAttribute();
}

void ConfigureHealthChecks(WebApplication app)
{
    // `/healthcheck` endpoint for manually diagnosing service health. Not associated with a Kubernetes probe.
    // Satisfies XREQ-168
    app.UseHealthCheckEndpoint("HealthCheck", route: "/healthcheck",
            check => check.Tags.Contains("health"))
        .WithOptionalSecretHeader();

    // `/ready` endpoint to signify whether the service is ready to receive traffic. Used by Kubernetes readiness probe.
    app.UseHealthCheckEndpoint("Ready", route: "/ready",
            check => check.Tags.Contains("ready"))
        .WithOptionalSecretHeader();

    // `/ping` endpoint for diagnosing service availability. Used by Kubernetes liveness probe.
    // Satisfies XREQ-167
    app.UseHealthCheckEndpoint("Ping", route: "/ping");

    // `/secure-ping` endpoint for diagnosing authentication configuration. Not associated with a Kubernetes probe.
    app.UseHealthCheckEndpoint("SecurePing", route: "/secure-ping")
        .RequireAuthorization();

    // "Include health check endpoints in the OpenAPI specification
    app.UseOpenApiDocumentFilter<HealthChecksDocumentFilter>(order: -100);
}

// =====================
// Core helper functions
// =====================

WebApplicationBuilder CreateWebApplicationBuilder(
    CatalogueMetadata catalogueMetadata)
{
    var acceleratorBuilder = WebApplication.CreateBuilder(args);

    // Ensure catalogue metadata is available before adding any other services
    acceleratorBuilder.Services.AddSingleton(catalogueMetadata);

    acceleratorBuilder.Configuration
        .AddEnvironmentVariables(prefix: "Override_");

    // Satisfies XREQ-291
    // Use Serilog for logging and read configuration from appsettings.json files
    acceleratorBuilder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
    );

    acceleratorBuilder.Services.AddControllers(options =>
    {
        // Satisfies XREQ-137 - Use kebab-case format for routes e.g. "/hello-world"
        options.Conventions.Add(new SlugifyParameterTransformerConvention());
        options.Filters.Add<ServiceFeatureFlagFilter>();
        options.Filters.Add<WhitelistAuthorizationFilter>();
    });

    // Satisfies XREQ-109 & XREQ-302 - Enable generation of OpenAPI specifications from code
    acceleratorBuilder.Services.AddOpenApiDocumentation();

    // Allow specific headers to be propagated to downstream services if the client is configured to do so.
    acceleratorBuilder.Services.AddHeaderPropagation(options =>
    {
        // Propagate Xero-Correlation-Id to correlate requests across multiple components for better observability
        options.Headers.Add(HttpHeaders.XeroCorrelationId);
        // Propagate Xero-User-Id to enable downstream APIs to perform authorisation
        options.Headers.Add(HttpHeaders.XeroUserId, context =>
        {
            var xeroUserIdFeature = context.HttpContext.Features.Get<XeroUserIdFeature>();
            if (xeroUserIdFeature != null)
            {
                return xeroUserIdFeature.XeroUserId.ToString();
            }

            return StringValues.Empty;
        });

        options.Headers.Add(HttpHeaders.XeroTenantId);
        options.Headers.Add(Constants.HttpHeaders.ProviderAccountId);
    });

    acceleratorBuilder.AddXeroErrorHandling();

    // Add a custom secret header authentication and authorisation scheme, used to secure health check endpoints.
    // Endpoints secured with SecretHeader can only be called with a specific header value
    // This value is configurable in appsettings.json `SecretHeader` block
    acceleratorBuilder.AddSecretHeader();

    acceleratorBuilder.AddXeroIdentityAuthentication(options =>
    {
        options.RetrieveUserIdFrom = XeroUserIdSource.Header;
    });

    // Satisfies XREQ-133
    // Configure Xero Authorisation (AuthZ). Modify the first argument to point to your own action map.
    acceleratorBuilder.AddXeroAuthorisation(
        Xero.Authorisation.Integration.Policy.Organisation.ApiServiceAccelerator.ActionMap,
        ComponentVersion.FromAssembly(),
        authBuilder => authBuilder.UseServiceAuthorisation()
    );

    return acceleratorBuilder;
}

void UseAcceleratorCoreServices(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseHostedOpenApiDocumentation();
    }
    // Return 400 if the incoming request does not have a Xero-Correlation-Id
    // You can use [AllowNoXeroCorrelationId] attribute or AllowNoXeroCorrelationId() on the endpoint to opt out
    // If a Xero-Correlation-Id header was not included in the incoming request, and not required for the endpoint, generate a new one.
    app.UseInboundXeroCorrelationIdMiddleware();

    // Automatically add Xero-User-Id, Xero-Tenant-Id, Xero-Correlation-Id to logs
    app.UseRequestLogging();

    // Developer exception page middleware is used in Development environment
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler();
    }

    // Satisfies XREQ-125/XREQ-129/XREQ-132
    app.UseXeroIdentityWithXeroUserId();

    // Enable Header Propagation for HttpClients
    // Details about which headers get propagated can be found in HttpRequestRegistration.cs
    app.UseHeaderPropagation();
}

// Class definition needed for Component Tests
public partial class Program //NOSONAR Non-protected or static partial class for Component Tests
{
}
