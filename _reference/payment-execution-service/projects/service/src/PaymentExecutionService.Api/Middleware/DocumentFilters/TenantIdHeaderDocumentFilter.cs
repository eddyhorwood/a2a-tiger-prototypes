using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;
using static Xero.Accelerators.Api.Core.Constants;

namespace PaymentExecutionService.Middleware;

public class TenantIdHeaderDocumentFilter(IEnumerable<EndpointDataSource> endpointDataSources) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operations = swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values);
        foreach (var operation in operations)
        {
            var endpoint = operation.FindEndpointForOperation(endpointDataSources);
            if (endpoint?.Metadata.GetMetadata<RequiresTenantIdHeaderAttribute>() != null)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = HttpHeaders.XeroTenantId,
                    In = ParameterLocation.Header,
                    Description = "Xero Tenant Id",
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                });
            }
        }
    }
}
