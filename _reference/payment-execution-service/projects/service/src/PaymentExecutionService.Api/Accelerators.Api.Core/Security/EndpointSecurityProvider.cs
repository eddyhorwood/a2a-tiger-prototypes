// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Xero.Accelerators.Api.Core.Security;

public class EndpointSecurityProvider(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IAuthorizationPolicyProvider authorizationPolicyProvider) : IEndpointSecurityProvider
{
    public async Task<EndpointSecurity> GetEndpointSecurityAsync(
        RouteEndpoint endpoint)
    {
        var authorisationAttribute = endpoint.Metadata.GetMetadata<IAuthorizeData>();
        if (authorisationAttribute is null || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return EndpointSecurity.None;
        }

        var policy = authorisationAttribute.Policy is null
            ? await authorizationPolicyProvider.GetDefaultPolicyAsync()
            : await authorizationPolicyProvider.GetPolicyAsync(authorisationAttribute.Policy);

        if (policy is null)
        {
            return EndpointSecurity.None;
        }

        var result = new EndpointSecurity();
        if (policy.AuthenticationSchemes.Any())
        {
            result.AuthenticationSchemes.AddRange(policy.AuthenticationSchemes);
        }
        else
        {
            var defaultScheme = await authenticationSchemeProvider.GetDefaultAuthenticateSchemeAsync();
            if (defaultScheme is not null)
            {
                result.AuthenticationSchemes.Add(defaultScheme.Name);
            }
        }

        var authorisationRequirements = policy.Requirements
            .Where(req => req is not DisabledAssertionRequirement);

        result.AuthorisationRequirements.AddRange(authorisationRequirements);

        return result;
    }
}

public class EndpointSecurity
{
    public static readonly EndpointSecurity None = new EndpointSecurity();

    public List<string> AuthenticationSchemes { get; }
    public List<IAuthorizationRequirement> AuthorisationRequirements { get; }

    public EndpointSecurity()
    {
        AuthenticationSchemes = new List<string>();
        AuthorisationRequirements = new List<IAuthorizationRequirement>();
    }
}
