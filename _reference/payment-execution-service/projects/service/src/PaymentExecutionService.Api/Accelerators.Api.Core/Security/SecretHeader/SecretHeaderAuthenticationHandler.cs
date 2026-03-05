// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Xero.Accelerators.Api.Core.Security.SecretHeader;

public class SecretHeaderAuthenticationHandler(
    IOptionsMonitor<SecretHeaderOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
    ) : AuthenticationHandler<SecretHeaderOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.SecretHeaderName))
        {
            return Task.FromResult(AuthenticateResult.Fail("Secret header name missing from config."));
        }

        if (string.IsNullOrWhiteSpace(Options.SecretKeys))
        {
            return Task.FromResult(AuthenticateResult.Fail("Secret header keys missing from config."));
        }

        if (!Request.Headers.ContainsKey(Options.SecretHeaderName))
        {
            return Task.FromResult(AuthenticateResult.Fail("Secret header missing."));
        }

        // Get authorization key
        var secretHeaderValue = Request.Headers[Options.SecretHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(secretHeaderValue))
        {
            return Task.FromResult(AuthenticateResult.Fail("Secret header value missing."));
        }

        var secretKeys = Options.SecretKeys.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!secretKeys.Any(s => s.Equals(secretHeaderValue)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Secret header value invalid."));
        }

        var authenticatedUser = new SecretHeaderAuthenticatedUser("SecretHeaderAuthentication", true, "X-Secret");
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(authenticatedUser));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name)));
    }
}


public class SecretHeaderAuthenticatedUser(string authenticationType, bool isAuthenticated, string name) : IIdentity
{
    public string AuthenticationType { get; } = authenticationType;

    public bool IsAuthenticated { get; } = isAuthenticated;

    public string Name { get; } = name;
}

public class SecretHeaderOptions : AuthenticationSchemeOptions
{
    /// <summary>
    ///     A comma delimited string of valid keys, e.g. "SECRET,PASSWORD".
    ///     Enables the cycling of a secret.
    /// </summary>
    public string SecretKeys { get; set; } = string.Empty;

    /// <summary>
    ///     The name of the header to compare against, e.g. "X-Secret"
    /// </summary>
    public string SecretHeaderName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the configuration key for Secret Header settings.
    /// </summary>
    public static string Key => "SecretHeader";
}


