namespace PaymentExecution.Domain.Models;

public class SubmittedPayment
{
    public Guid ProviderServiceId { get; set; }
    public required string PaymentIntentId { get; set; }
    public required string ClientSecret { get; set; }
    public int TtlInSeconds { get; set; }
}
