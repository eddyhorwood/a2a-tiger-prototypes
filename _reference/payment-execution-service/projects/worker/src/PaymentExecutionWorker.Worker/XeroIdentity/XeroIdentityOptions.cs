namespace PaymentExecutionWorker.XeroIdentity;

#nullable enable
public class XeroIdentityOptions
{
    public static string Key => "Identity";
    public string? Authority { get; set; }

    public IdentityClientOptions? Client { get; set; }

    public IdentityAuthenticationOptions? Authentication { get; set; }

    public bool RequireHttpsMetadata
    {
        get
        {
            if (Uri.TryCreate(Authority, UriKind.Absolute, out var authorityUri))
            {
                return string.Equals(authorityUri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}

public class IdentityClientOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? ClientScopes { get; set; }
}

public class IdentityAuthenticationOptions
{
    public string[]? RequiredScopes { get; set; }
}
