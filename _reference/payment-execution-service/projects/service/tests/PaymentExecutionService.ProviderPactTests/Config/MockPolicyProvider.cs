using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PaymentExecutionService.ProviderPactTests.Config;

public class MockPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireClaim("foo")
            .Build();
        return Task.FromResult(policy)!;
    }
}
