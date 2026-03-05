// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xero.Accelerators.Api.Core.Security;

namespace Xero.Accelerators.Api.Core.OpenApi;

public static class OpenApiDocumentationExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, Action<OpenApiDocumentationOptions>? configure = null)
    {
        services.AddScoped<IOpenApiDocumentationContext, OpenApiDocumentationContext>();
        services.AddSingleton<IEndpointSecurityProvider, EndpointSecurityProvider>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.DocumentFilter<OpenApiRootDocumentFilter>();
            options.SupportNonNullableReferenceTypes();
            options.UseAllOfToExtendReferenceSchemas();
            options.AddSwaggerDocumentation();
        });

        services.Configure<OpenApiDocumentationOptions>(options =>
        {
            options.AddFilter<ApiInfoDocumentFilter>(order: 0);
            options.AddFilter<XeroTenantRouteDocumentFilter>(order: 100);
            options.AddFilter<AuthorisationDocumentFilter>(order: 500);
            options.AddFilter<BadRequestDocumentFilter>(order: 600);
            options.AddFilter<MissingOperationIdDocumentFilter>(order: 1100);
            configure?.Invoke(options);
        });

        return services;
    }

    private static void AddSwaggerDocumentation(this SwaggerGenOptions options)
    {
        var xmlDocFiles = Directory
            .GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file)
            .ToArray();
        foreach (var xmlFile in xmlDocFiles)
        {
            options.IncludeXmlComments(xmlFile);
        }
    }

    public static IServiceCollection AddOpenApiDocumentFilter<T>(this IServiceCollection services, int order, params object[] arguments) where T : IDocumentFilter
    {
        services.Configure<OpenApiDocumentationOptions>(options =>
        {
            options.AddFilter<T>(order: order, arguments);
        });

        return services;
    }

    public static IApplicationBuilder UseHostedOpenApiDocumentation(this WebApplication app)
    {
        var specPath = "/swagger/v1/swagger.yaml";
        // expose the OpenAPI spec YAML file via an endpoint
        app.UseSwagger();
        app.UseSwaggerUI();

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var appUrl = app.Urls.FirstOrDefault();
            app.Logger.LogInformation("Serving OpenAPI spec at {url}", $"{appUrl}{specPath}");
        });

        return app;
    }

    /// <summary>
    /// Add a document filter that can be used to customise the generated OpenAPI Spec.
    /// </summary>
    /// <typeparam name="T">The document filter type (must implement <see cref="IDocumentFilter"/>)</typeparam>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="order">
    ///     A value used to determined the order in which the filter is run.
    ///     <para>
    ///         Document filters are executed in ascending order.
    ///         To ensure document filters run properly, we recommend selecting a value within the following ranges:
    ///         <list type="bullet">
    ///             <item>
    ///                 <term>-1000 to -1</term>
    ///                 <description>Filters that provide additional operations (e.g. operations for healthcheck endpoints).</description>
    ///             </item>
    ///             <item>
    ///                 <term>0 to 1000</term>
    ///                 <description>Filters that annotate existing operations (e.g. add required headers).</description>
    ///             </item>
    ///             <item>
    ///                 <term>1001 to 2000</term>
    ///                 <description>Filters that validate the final document.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </param>
    /// <param name="arguments">Constructor arguments to create an instance of the filter.</param>
    public static IApplicationBuilder UseOpenApiDocumentFilter<T>(this IApplicationBuilder builder, int order, params object[] arguments) where T : IDocumentFilter
    {
        var optionsMonitor = builder.ApplicationServices.GetService<IOptionsMonitor<OpenApiDocumentationOptions>>();

        if (optionsMonitor is not null)
        {
            var options = optionsMonitor.CurrentValue;
            options.AddFilter<T>(order, arguments);
        }

        return builder;
    }

    public static OpenApiOperation? FindOperationForRoute(this OpenApiDocument doc, RouteEndpoint endpoint)
    {
        var nameMeta = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        if (nameMeta == null)
        {
            return null;
        }

        var operations = doc.Paths.Values.SelectMany(path => path.Operations.Values);
        var matchedOperations = operations.Where(op => op.OperationId == nameMeta.EndpointName).ToList();

        // There should be a 1:1 relationship between an endpoint and an operation
        Debug.Assert(matchedOperations.Count <= 1);
        return matchedOperations.FirstOrDefault();
    }

    public static RouteEndpoint? FindEndpointForOperation(this OpenApiOperation operation, IEnumerable<RouteEndpoint> endpoints)
    {
        var matchedEndpoints = endpoints.Where(
            endpoint =>
            {
                var meta = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
                return meta?.EndpointName == operation.OperationId;
            })
            .ToList();

        // There should be a 1:1 relationship between an endpoint and an operation
        Debug.Assert(matchedEndpoints.Count <= 1);
        return matchedEndpoints.FirstOrDefault();
    }

    public static RouteEndpoint? FindEndpointForOperation(this OpenApiOperation operation, IEnumerable<EndpointDataSource> endpointDataSources)
    {
        var endpoints = endpointDataSources
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>();

        return FindEndpointForOperation(operation, endpoints);
    }
}
