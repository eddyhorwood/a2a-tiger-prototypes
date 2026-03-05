using System.Text.Json.Serialization;

namespace PaymentExecution.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TerminalStatus
{
    Succeeded,
    Failed,
    Cancelled
}
