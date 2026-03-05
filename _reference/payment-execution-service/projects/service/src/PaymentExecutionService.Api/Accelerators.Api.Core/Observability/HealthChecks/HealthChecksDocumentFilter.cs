// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Observability.HealthChecks;

public class HealthChecksDocumentFilter(
    IOpenApiDocumentationContext ctx,
    IEnumerable<EndpointDataSource> endpointDataSources) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var endpoints = endpointDataSources
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>();
        foreach (var endpoint in endpoints)
        {
            var rawEndpointRoute = endpoint.RoutePattern.RawText;
            if (string.IsNullOrWhiteSpace(rawEndpointRoute))
            {
                continue;
            }
            var route = rawEndpointRoute!.StartsWith('/')
                ? rawEndpointRoute
                : $"/{rawEndpointRoute}";

            var endpointHealthCheckMetadata = endpoint.Metadata.GetMetadata<HealthCheckMetadata>();
            if (endpointHealthCheckMetadata is not null)
            {
                var nameMeta = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
                if (nameMeta == null)
                {
                    ctx.Warn(
                        "Health check endpoint at route '{0}' is missing an `EndpointNameMetadata`.", route);
                    continue;
                }

                var operation = new OpenApiOperation
                {
                    OperationId = nameMeta.EndpointName
                };

                foreach (var statusCodeGroup in endpointHealthCheckMetadata.ResultStatusCodes.GroupBy(kvp => kvp.Value))
                {
                    operation.Responses.Add(statusCodeGroup.Key.ToString(), new OpenApiResponse
                    {
                        Description = string.Join(" / ", statusCodeGroup.Select(kvp => kvp.Key))
                    });
                }

                operation.Tags.Add(new OpenApiTag { Name = endpoint.DisplayName });

                var path = new OpenApiPathItem();
                path.Operations[OperationType.Get] = operation;

                swaggerDoc.Paths.Add(route, path);
            }
        }
    }
}
