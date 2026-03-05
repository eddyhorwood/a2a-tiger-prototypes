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

namespace Xero.Accelerators.Api.Core.OpenApi;

public class MissingOperationIdDocumentFilter(IOpenApiDocumentationContext ctx) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in swaggerDoc.Paths)
        {
            foreach (var operation in path.Value.Operations) //NOSONAR for readability
            {
                if (string.IsNullOrWhiteSpace(operation.Value.OperationId))
                {
                    var httpMethod = operation.Key.ToString().ToUpper();
                    var route = path.Key;
                    ctx.Warn("{0} operation at route '{1}' is missing an operation ID.", httpMethod, route);
                }
            }
        }
    }
}
