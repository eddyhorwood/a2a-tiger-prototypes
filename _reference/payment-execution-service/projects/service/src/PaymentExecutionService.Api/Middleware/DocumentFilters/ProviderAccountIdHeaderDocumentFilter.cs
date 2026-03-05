using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;

namespace PaymentExecutionService.Middleware;

public class ProviderAccountIdHeaderDocumentFilter(IEnumerable<EndpointDataSource> endpointDataSources) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operations = swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values);
        foreach (var operation in operations)
        {
            var endpoint = operation.FindEndpointForOperation(endpointDataSources);
            if (endpoint?.Metadata.GetMetadata<RequiresProviderAccountIdHeaderAttribute>() != null)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = Constants.HttpHeaders.ProviderAccountId,
                    In = ParameterLocation.Header,
                    Description = "Provider Account Id",
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
            }
        }
    }
}
