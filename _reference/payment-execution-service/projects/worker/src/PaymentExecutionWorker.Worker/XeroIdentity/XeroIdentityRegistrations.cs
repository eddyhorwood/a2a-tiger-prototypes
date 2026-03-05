namespace PaymentExecutionWorker.XeroIdentity;

public static class XeroIdentityRegistrations
{
    public static IServiceCollection AddXeroIdentityRegistration(this IServiceCollection services,
        IConfiguration configuration)
    {
        var identityConfig = configuration.GetSection(XeroIdentityOptions.Key).Get<XeroIdentityOptions>();

        services.AddXeroIdentityClient(options =>
        {
            options.Authority = identityConfig!.Authority;
            options.ClientId = identityConfig.Client!.ClientId;
            options.ClientSecret = identityConfig.Client.ClientSecret;
            options.Scopes = identityConfig.Client.ClientScopes;
            options.RequireHttpsMetadata = identityConfig.RequireHttpsMetadata;
        });

        return services;
    }
}
