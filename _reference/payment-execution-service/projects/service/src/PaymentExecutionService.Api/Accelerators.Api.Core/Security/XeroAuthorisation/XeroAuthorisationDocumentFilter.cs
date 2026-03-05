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
using Xero.Accelerators.Api.Core.OpenApi;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Authorisation.Integration.NetCore.Sdk.Authorize;
using static Xero.Accelerators.Api.Core.Constants;

namespace Xero.Accelerators.Api.Core.Security.XeroAuthorisation;

public class XeroAuthorisationDocumentFilter(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IEndpointSecurityProvider endpointSecurityProvider,
    IOptionsMonitor<XeroIdentityOptions> identityOptions)
    : IDocumentFilter
{
    private readonly XeroIdentityOptions _identityOptions = identityOptions.CurrentValue;

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

            if (operation.Parameters.Any(p => p.Name == HttpHeaders.XeroUserId))
            {
                continue;
            }

            var auth = endpointSecurityProvider
                .GetEndpointSecurityAsync(endpoint)
                .GetAwaiter()
                .GetResult();

            if (!auth.AuthorisationRequirements.OfType<AssertionRequirement>().Any())
            {
                continue;
            }

            if (_identityOptions.RetrieveUserIdFrom.HasFlag(XeroUserIdSource.Header))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = HttpHeaders.XeroUserId,
                    In = ParameterLocation.Header,
                    Description = "Xero User Id",
                    // The header is required if Header is the only source to get Xero-User-Id 
                    // Otherwise, the header is not required if Xero-User-Id can be retrieved from other sources (e.g. claims)
                    Required = _identityOptions.RetrieveUserIdFrom == XeroUserIdSource.Header,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
                });
            }
        }
    }
}
