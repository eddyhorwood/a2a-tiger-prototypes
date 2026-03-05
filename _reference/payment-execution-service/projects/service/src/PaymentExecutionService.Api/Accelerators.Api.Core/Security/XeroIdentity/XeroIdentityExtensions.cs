// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Security.XeroIdentity;

[ExcludeFromCodeCoverage]
public static class XeroIdentityExtensions
{
    public static void AddXeroIdentityAuthentication(this WebApplicationBuilder webApplicationBuilder, Action<XeroIdentityOptions> configure)
    {
        var identityConfig = webApplicationBuilder.Configuration.GetSection(IdentityOptions.Key).Get<IdentityOptions>();
        var catalogueMetadata =
            webApplicationBuilder.Services.BuildServiceProvider()
                .GetService<CatalogueMetadata>();

        webApplicationBuilder.Services.AddXeroIdentityAuthentication(options =>
        {
            options.Authority = identityConfig!.Authority;
            options.ApiName = catalogueMetadata?.Name.Replace(" ", "");
            options.RequireHttpsMetadata = identityConfig.RequireHttpsMetadata;
        }).AddScopeAuthorization(identityConfig!.Authentication!.RequiredScopes);

        webApplicationBuilder.EnableMultipleAuthoritiesInTestEnvironment();
        webApplicationBuilder.Services.Configure(configure);
    }

    public static void AddXeroIdentityClient(this WebApplicationBuilder webApplicationBuilder)
    {
        var identityConfig = webApplicationBuilder.Configuration.GetSection(IdentityOptions.Key).Get<IdentityOptions>();

        // Satisfies XREQ-131
        webApplicationBuilder.Services.AddXeroIdentityClient(options =>
        {
            options.Authority = identityConfig!.Authority;
            options.ClientId = identityConfig.Client!.ClientId;
            options.ClientSecret = identityConfig.Client.ClientSecret;
            options.Scopes = identityConfig.Client.ClientScopes;
            options.RequireHttpsMetadata = identityConfig.RequireHttpsMetadata;
        });
    }

    public static IApplicationBuilder UseXeroIdentityWithXeroUserId(this IApplicationBuilder builder)
    {
        // `UseXeroIdentity()` already calls both UseAuthentication() and UseAuthorisation()
        // https://github.dev.xero.com/identity/identity.sdks/blob/4ab1fbd18fba4a1cfb079d370403bf972d74dd13/src/Xero.Identity.Integration.Api/ApplicationBuilder/XeroApplicationBuilderExtensions.cs#L20-L24
        // However, `XeroUserIdMiddleware` needs to run after `UseAuthentication()` (to have the claims populated in HttpContext)
        // but before `UseAuthorization()` (to have the Xero User Id in the http context feature for AuthZ's ContextProvider)
        // So we manually call `UseAuthentication()` here and slot in `UseMiddleware<XeroUserIdMiddleware>()`
        // before `UseXeroIdentity()`
        builder.UseAuthentication();
        builder.UseMiddleware<XeroUserIdMiddleware>();
        builder.UseXeroIdentity();
        builder.UseOpenApiDocumentFilter<XeroIdentityDocumentFilter>(order: 400);
        return builder;
    }

    /// <summary>
    /// This test disabled issuer, audience validation in test environment ONLY.
    /// This allows us to accept tokens from many authorities in the test environment. eg fringe2, 4 
    /// - Issuer validation ensures the token was delivered by the configured authority 
    /// - Audience validation ensure the token was created FOR this resource.
    /// </summary>
    /// <param name="webApplicationBuilder"></param>
    private static void EnableMultipleAuthoritiesInTestEnvironment(this WebApplicationBuilder webApplicationBuilder)
    {
        if (!webApplicationBuilder.Environment.IsEnvironment("test"))
        {
            return;
        }

        webApplicationBuilder.Services.Configure<JwtBearerOptions>(
            "BearerIdentityServerAuthenticationJwt",
            o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
    }
}
