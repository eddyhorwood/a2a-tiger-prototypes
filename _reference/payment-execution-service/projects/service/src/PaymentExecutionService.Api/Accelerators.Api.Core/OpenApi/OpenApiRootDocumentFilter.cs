// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Xero.Accelerators.Api.Core.OpenApi;

public class OpenApiRootDocumentFilter(
    IServiceProvider serviceProvider,
    IOptionsMonitor<OpenApiDocumentationOptions> options) : IDocumentFilter
{
    private readonly IOptionsMonitor<OpenApiDocumentationOptions> _options = options;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var options = _options.CurrentValue;

        using var scope = serviceProvider.CreateScope();
        foreach (var descriptor in options.DocumentFilters)
        {
            var filter = (IDocumentFilter)ActivatorUtilities.CreateInstance(
                scope.ServiceProvider, descriptor.Type, descriptor.Arguments);
            filter.Apply(swaggerDoc, context);
        }
    }
}
