namespace PaymentExecution.StripeExecutionClient.Contracts.Models;

public class StripeExeSubmitPaymentResponseDto
{
    public Guid ProviderServiceId { get; set; }
    public required string PaymentIntentId { get; set; }
    public required string ClientSecret { get; set; }
    public int TtlInSeconds { get; set; }
}
