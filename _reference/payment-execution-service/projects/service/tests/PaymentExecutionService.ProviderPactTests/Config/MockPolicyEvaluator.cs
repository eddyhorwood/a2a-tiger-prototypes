using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace PaymentExecutionService.ProviderPactTests.Config;

public class MockPolicyEvaluator : IPolicyEvaluator
{
    public async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        const string Scheme = "MockScheme";
        var principal = new ClaimsPrincipal();
        principal.AddIdentity(new ClaimsIdentity(new[] {
            new Claim("foo", "bar")
        }, Scheme));

        return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,
            new AuthenticationProperties(), Scheme)));
    }

    public async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy,
        AuthenticateResult authenticationResult, HttpContext context, object? resource)
    {
        if (context.Request.Path.Value!.StartsWith("/provider-states"))
        {
            return await Task.FromResult(PolicyAuthorizationResult.Success());
        }

        return context.Request.Headers.Authorization.Count == 0
            ? await Task.FromResult(PolicyAuthorizationResult.Forbid())
            : await Task.FromResult(PolicyAuthorizationResult.Success());
    }
}
