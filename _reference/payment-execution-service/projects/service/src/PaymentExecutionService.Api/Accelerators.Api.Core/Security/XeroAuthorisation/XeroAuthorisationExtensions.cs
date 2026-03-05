// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Xero.Accelerators.Api.Core.Conventions.Versioning;
using Xero.Accelerators.Api.Core.OpenApi;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Authorisation.Integration.NetCore.Sdk;

namespace Xero.Accelerators.Api.Core.Security.XeroAuthorisation;

public static class XeroAuthorisationExtensions
{
    public static void AddXeroAuthorisation(
        this WebApplicationBuilder webApplicationBuilder,
        IEnumerable<KeyValuePair<string, Authorisation.Integration.Common.Action>> actionMap,
        ComponentVersion componentVersion,
        Action<NetCoreAuthorisationBuilder> configureAuth
    )
    {
        AddXeroAuthorisation<ContextProvider>(webApplicationBuilder, actionMap, componentVersion, configureAuth);
    }

    public static void AddXeroAuthorisation<TContextProvider>(
        this WebApplicationBuilder webApplicationBuilder,
        IEnumerable<KeyValuePair<string, Authorisation.Integration.Common.Action>> actionMap,
        ComponentVersion componentVersion,
        Action<NetCoreAuthorisationBuilder> configureAuth)
        where TContextProvider : class, Authorisation.Integration.Common.IContextProvider
    {
        webApplicationBuilder.Services.AddHttpContextAccessor();
        webApplicationBuilder.AddXeroIdentityClient();

        webApplicationBuilder.Services.AddXeroAuthorisation(authBuilder =>
        {
            authBuilder.RetrieveConfigurationFrom(webApplicationBuilder.Configuration.GetSection("Authorisation"));
            configureAuth(authBuilder);
            authBuilder
                .ForClientApp(componentVersion.Version!, componentVersion.GitHash!)
                .WithContextProvider<TContextProvider>()
                .UseASPNETAuthorizationAttributes(actionMap);
        });

        webApplicationBuilder.Services.AddOpenApiDocumentFilter<XeroAuthorisationDocumentFilter>(order: 400);
    }
}
