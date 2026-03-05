using System.Text.Json.Serialization;

namespace PaymentExecution.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderType
{
    Stripe,
    Paypal,
    GoCardless,
}
