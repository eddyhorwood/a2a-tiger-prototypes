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

namespace Xero.Accelerators.Api.Core.Conventions.Routing;

public class XeroTenantRouteDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operations = swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values);

        foreach (var operation in operations)
        {
            var tenantIdParameters = operation.Parameters.Where(parameter => parameter.Name == "xeroTenantId" && parameter.In == ParameterLocation.Path);

            foreach (var parameter in tenantIdParameters)
            {
                parameter.Description = "Xero Tenant Id";
                parameter.Schema.Format = "uuid";
            }
        }
    }
}
