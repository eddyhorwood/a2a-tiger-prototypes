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
using static Xero.Accelerators.Api.Core.Constants;

namespace Xero.Accelerators.Api.Core.Observability.Correlation;

public class CorrelationIdDocumentFilter(
    IEnumerable<EndpointDataSource> endpointDataSources) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operations = swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values);
        foreach (var operation in operations)
        {
            var endpoint = operation.FindEndpointForOperation(endpointDataSources);
            if (endpoint?.Metadata.GetMetadata<AllowNoXeroCorrelationIdAttribute>() != null)
            {
                continue;
            }

            // Check if XeroCorrelationId parameter already exists
            var hasCorrelationIdParam = operation.Parameters.Any(p =>
                p.Name == HttpHeaders.XeroCorrelationId &&
                p.In == ParameterLocation.Header);

            if (!hasCorrelationIdParam)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = HttpHeaders.XeroCorrelationId,
                    In = ParameterLocation.Header,
                    Description = "Xero Correlation Id",
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                });
            }
        }
    }
}
