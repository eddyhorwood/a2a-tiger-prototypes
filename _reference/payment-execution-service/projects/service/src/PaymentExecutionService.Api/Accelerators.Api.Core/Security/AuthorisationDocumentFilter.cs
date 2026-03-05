// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Security;

public class AuthorisationDocumentFilter(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IEndpointSecurityProvider endpointSecurityProvider) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var endpoints = endpointDataSources
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>();

        foreach (var endpoint in endpoints)
        {
            var operation = swaggerDoc.FindOperationForRoute(endpoint);
            if (operation == null)
            {
                continue;
            }

            var auth = endpointSecurityProvider
                .GetEndpointSecurityAsync(endpoint)
                .GetAwaiter()
                .GetResult();

            if (auth.AuthenticationSchemes.Any())
            {
                operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            }

            /*
            If the only authorisation requirement is that the user must be authenticated
            (`DenyAnonymousAuthorizationRequirement`), it's not possible for the service to return a 403.
            If the user is not authenticated, the service will return a 401 instead.
            */
            if (auth.AuthorisationRequirements.Any(req => req is not DenyAnonymousAuthorizationRequirement))
            {
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
            }
        }
    }
}
