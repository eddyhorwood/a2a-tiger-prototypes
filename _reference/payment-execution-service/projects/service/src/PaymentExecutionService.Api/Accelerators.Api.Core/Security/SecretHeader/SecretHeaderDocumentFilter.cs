// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Security.SecretHeader;

public class SecretHeaderDocumentFilter(
    IEnumerable<EndpointDataSource> endpointDataSources,
    IEndpointSecurityProvider endpointSecurityProvider,
    IOptions<SecretHeaderOptions> secretHeaderOptions,
    string authenticationSchemeName) : IDocumentFilter
{
    private readonly SecretHeaderOptions _secretHeaderOptions = secretHeaderOptions.Value;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var anyEndpointsRequireSecretHeader = false;
        var secretHeaderScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Id = "SecretHeader", Type = ReferenceType.SecurityScheme },
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = _secretHeaderOptions.SecretHeaderName
        };

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

            if (auth.AuthenticationSchemes.Contains(authenticationSchemeName))
            {
                var isSecretHeaderRequired = false;

                if (auth.AuthorisationRequirements.OfType<DenyAnonymousAuthorizationRequirement>().Any())
                {
                    anyEndpointsRequireSecretHeader = true;
                    isSecretHeaderRequired = true;
                    operation.Security.Add(new OpenApiSecurityRequirement()
                    {
                        { secretHeaderScheme, Array.Empty<string>() }
                    });
                }

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = _secretHeaderOptions.SecretHeaderName,
                    In = ParameterLocation.Header,
                    Required = isSecretHeaderRequired,
                    Description = "A secret value known only by the service owners.",
                    Schema = new OpenApiSchema() { Type = "string" }
                });
            }
        }

        if (anyEndpointsRequireSecretHeader)
        {
            swaggerDoc.Components.SecuritySchemes.Add(secretHeaderScheme.Reference.Id, secretHeaderScheme);
        }
    }
}
