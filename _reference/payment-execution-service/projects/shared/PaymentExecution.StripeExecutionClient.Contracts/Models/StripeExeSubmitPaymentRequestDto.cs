namespace PaymentExecution.StripeExecutionClient.Contracts.Models;

public class StripeExeSubmitPaymentRequestDto
{
    public required StripeExePaymentRequestDto PaymentRequest { get; set; }

    public List<string>? PaymentMethodsMadeAvailable { get; set; }

    public string? PaymentMethodId { get; set; }
}
