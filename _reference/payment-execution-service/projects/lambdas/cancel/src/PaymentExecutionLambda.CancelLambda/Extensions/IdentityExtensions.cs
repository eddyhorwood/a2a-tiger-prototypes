using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecutionLambda.CancelLambda.Models;

namespace PaymentExecutionLambda.CancelLambda.Extensions;

public static class IdentityExtensions
{
    public static void ConfigureXeroIdentityClient(this IServiceCollection services, IConfiguration configuration)
    {
        var identityConfig = configuration.GetSection(IdentityOptions.Key).Get<IdentityOptions>();

        services.AddXeroIdentityClient(options =>
        {
            options.Authority = identityConfig!.Authority;
            options.ClientId = identityConfig.Client!.ClientId;
            options.ClientSecret = identityConfig.Client.ClientSecret;
            options.Scopes = identityConfig.Client.ClientScopes;
            options.RequireHttpsMetadata = identityConfig.Authority!.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        });
    }
}

