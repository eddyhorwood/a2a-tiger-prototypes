namespace PaymentExecution.PaymentRequestClient.Models;

public record SelectedPaymentMethod
{
    public required Guid PaymentGatewayId { get; set; }
    public required string PaymentMethodName { get; set; }
    public decimal? SurchargeAmount { get; set; }
}
