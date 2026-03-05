namespace PaymentExecutionLambda.CancelLambda.Models;

public class IdentityOptions
{
    public static readonly string Key = "Identity";

    public string? Authority { get; set; }

    public IdentityClientOptions? Client { get; set; }
}

public class IdentityClientOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? ClientScopes { get; set; }
}

