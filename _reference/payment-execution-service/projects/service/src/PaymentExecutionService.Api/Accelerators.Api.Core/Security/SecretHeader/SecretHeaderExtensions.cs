// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Authorization;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Security.SecretHeader;

public static class SecretHeaderExtensions
{
    private static readonly string _secretHeaderAuthenticationScheme = "SecretHeaderAuthentication";
    private static readonly string _secretHeaderAuthorizationPolicy = "SecretHeaderAuthentication";
    private static readonly string _optionalSecretHeaderAuthorizationPolicy = "OptionalSecretHeaderAuthentication";

    public static void AddSecretHeader(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, DisabledAuthorizationHandler>();

        builder.Services.Configure<SecretHeaderOptions>(
            builder.Configuration.GetSection(SecretHeaderOptions.Key));

        builder.Services.AddAuthentication()
            .AddScheme<SecretHeaderOptions, SecretHeaderAuthenticationHandler>(
                _secretHeaderAuthenticationScheme,
                "Secret Header Authentication",
                options => { builder.Configuration.Bind(SecretHeaderOptions.Key, options); });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(_secretHeaderAuthorizationPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AuthenticationSchemes.Add(_secretHeaderAuthenticationScheme);
            });
            options.AddPolicy(_optionalSecretHeaderAuthorizationPolicy, policy =>
            {
                policy.Requirements.Add(new DisabledAssertionRequirement());
                policy.AuthenticationSchemes.Add(_secretHeaderAuthenticationScheme);
            });
        });

        builder.Services.AddOpenApiDocumentFilter<SecretHeaderDocumentFilter>(order: 700, _secretHeaderAuthenticationScheme);
    }

    public static TBuilder RequireSecretHeader<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(_secretHeaderAuthorizationPolicy);
    }

    public static TBuilder WithOptionalSecretHeader<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(_optionalSecretHeaderAuthorizationPolicy);
    }
}
