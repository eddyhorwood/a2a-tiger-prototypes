using System.Text.Json.Serialization;

namespace PaymentExecution.PaymentRequestClient.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceivableType
{
    invoice
}
