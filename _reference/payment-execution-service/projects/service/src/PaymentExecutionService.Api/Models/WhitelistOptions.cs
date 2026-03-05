using System.Configuration;

namespace PaymentExecutionService.Models;

public class WhitelistOptions
{
    public const string Key = "Whitelist";
    public List<string> ClientIds { get; set; } = new List<string>();
}

public static class WhitelistRegistration
{
    public static void AddServiceWhitelistOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var optsConfig = configuration.GetSection(WhitelistOptions.Key);
        var opts = optsConfig.Get<WhitelistOptions>();

        if (opts == null || opts.ClientIds.Count == 0)
        {
            throw new ConfigurationErrorsException($"Configuration section '{WhitelistOptions.Key}' is missing or 'ClientIds' must not be empty.");
        }

        services.Configure<WhitelistOptions>(optsConfig);
    }
}
