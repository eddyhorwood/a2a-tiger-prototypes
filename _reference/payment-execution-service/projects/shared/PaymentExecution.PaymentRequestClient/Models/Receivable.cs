using PaymentExecution.PaymentRequestClient.Models.Enums;

namespace PaymentExecution.PaymentRequestClient.Models;

public record Receivable
{
    public required Guid Identifier { get; set; }
    public required ReceivableType Type { get; set; }
}
