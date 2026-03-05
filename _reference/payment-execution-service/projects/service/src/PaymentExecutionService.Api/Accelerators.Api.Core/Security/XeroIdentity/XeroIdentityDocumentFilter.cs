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

namespace Xero.Accelerators.Api.Core.Security.XeroIdentity;

public class XeroIdentityDocumentFilter(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IEndpointSecurityProvider endpointSecurityProvider
        ) : IDocumentFilter
{
    // Defined in Xero.Identity.Integration.Api
    private static readonly string _xeroIdentityAuthenticationSchemeName = "Bearer";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var xeroIdentityScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Id = "XeroIdentity",
                Type = ReferenceType.SecurityScheme
            },
            Type = SecuritySchemeType.OAuth2,
            Description = "Xero Identity Security Scheme",
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute), //NOSONAR Identity URL is hardcoded
                    RefreshUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute) //NOSONAR Identity URL is hardcoded
                }
            }
        };

        var createEndpointSecurityOperations = new CreateEndpointSecurityOperations
        {
            AllIdentityScopes = new List<string>(),
            AllAuthenticationSchemes = new List<string>(),
            SwaggerDoc = swaggerDoc,
            XeroIdentityScheme = xeroIdentityScheme
        };

        AddEndpointSecurityOperations(createEndpointSecurityOperations);

        if (createEndpointSecurityOperations.AllAuthenticationSchemes.Contains(_xeroIdentityAuthenticationSchemeName))
        {
            xeroIdentityScheme.Flows.ClientCredentials.Scopes = createEndpointSecurityOperations.AllIdentityScopes.Distinct().ToDictionary(value => value, value => value);
            swaggerDoc.Components.SecuritySchemes.Add(xeroIdentityScheme.Reference.Id, xeroIdentityScheme);
        }
    }

    private void AddEndpointSecurityOperations(CreateEndpointSecurityOperations createEndpointSecurityOperations)
    {
        var endpoints = endpointDataSources
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>();
        foreach (var endpoint in endpoints)
        {
            var operation = createEndpointSecurityOperations.SwaggerDoc!.FindOperationForRoute(endpoint);
            if (operation == null)
            {
                continue;
            }

            var auth = endpointSecurityProvider
                .GetEndpointSecurityAsync(endpoint)
                .GetAwaiter()
                .GetResult();

            createEndpointSecurityOperations.AllAuthenticationSchemes!.AddRange(auth.AuthenticationSchemes);

            var endpointIdentityScopes = new List<string>();
            if (auth.AuthorisationRequirements.Any())
            {
                foreach (var requirement in auth.AuthorisationRequirements)
                {
                    if (requirement is ClaimsAuthorizationRequirement { ClaimType: "scope", AllowedValues: not null } claims)
                    {
                        endpointIdentityScopes.AddRange(claims.AllowedValues);
                        createEndpointSecurityOperations.AllIdentityScopes!.AddRange(claims.AllowedValues);
                    }
                }
            }

            if (endpointIdentityScopes.Any())
            {
                operation.Security.Add(new OpenApiSecurityRequirement()
                {
                    { createEndpointSecurityOperations.XeroIdentityScheme!, endpointIdentityScopes }
                });
            }
        }
    }

    private class CreateEndpointSecurityOperations //NOSONAR for sealed modifier
    {
        public OpenApiDocument? SwaggerDoc { get; set; }
        public OpenApiSecurityScheme? XeroIdentityScheme { get; set; }
        public List<string>? AllIdentityScopes { get; set; }
        public List<string>? AllAuthenticationSchemes { get; set; }
    }
}
