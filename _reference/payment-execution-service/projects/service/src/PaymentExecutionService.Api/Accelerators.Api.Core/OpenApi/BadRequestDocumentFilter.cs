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

public class BadRequestDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operations = swaggerDoc.Paths.Values.SelectMany(path => path.Operations.Values);

        foreach (var operation in operations)
        {
            if (operation.Parameters.Any() || operation.RequestBody != null)
            {
                operation.Responses.TryAdd("400", new OpenApiResponse
                {
                    Description = "Bad Request"
                });
            }
        }
    }
}
