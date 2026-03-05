// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;

namespace Xero.Accelerators.Api.Core.OpenApi;

public class ApiInfoDocumentFilter(CatalogueMetadata catalogueMetadata,
    IOpenApiDocumentationContext ctx) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (catalogueMetadata.EnvironmentUrls.Any())
        {
            foreach (var (environment, url) in catalogueMetadata
                         .EnvironmentUrls)
            {
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    swaggerDoc.Servers.Add(new OpenApiServer
                    {
                        Url = url,
                        Description = $"{environment} URL"
                    });
                }
                else
                {
                    ctx.Warn(
                        "OpenAPI spec {0} Url is not valid. Please configure correct URLs for `CatalogueMetadata` in `Program.cs`.",
                        environment);
                }
            }
        }
        else
        {
            ctx.Warn(
                "OpenAPI spec is missing Server URLs. Please configure `EnvironmentUrls` for `CatalogueMetadata` in `Program.cs`.");
        }

        if (!string.IsNullOrWhiteSpace(catalogueMetadata.Name))
        {
            swaggerDoc.Info.Title = catalogueMetadata.Name;
        }
        else
        {
            ctx.Warn(
                "OpenAPI spec Title cannot be empty. Please configure a `Name` for `CatalogueMetadata` in `Program.cs`.");
        }

        if (!string.IsNullOrWhiteSpace(catalogueMetadata.Description))
        {
            swaggerDoc.Info.Description = catalogueMetadata.Description;
        }
        else
        {
            ctx.Warn(
                "OpenAPI spec Description cannot be empty. Please configure a `Description` for `CatalogueMetadata` in `Program.cs`.");
        }

        swaggerDoc.Info.Contact = new OpenApiContact
        {
            Url = new Uri(
                $"https://app.getcortexapp.com/admin/service?tenantCode=Xero&tag={catalogueMetadata.ComponentUuid}/")
        };

        swaggerDoc.Info.Extensions["x-xero-api-type"] =
            new OpenApiString(catalogueMetadata.ApiType.ToString());
    }
}
