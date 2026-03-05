namespace PaymentExecution.StripeExecutionClient.Contracts.Models;

public class StripeExePaymentRequestDto
{
    public required Guid PaymentRequestId { get; set; }
    public required Guid OrganisationId { get; set; }
    public required StripeExeBillingContactDetailsDto BillingContactDetails { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string PaymentDescription { get; set; }
    public required List<StripeExeLineItemDto> LineItems { get; set; }
    public required string Executor { get; set; }
    public required string MerchantReference { get; set; }
    public required StripeExeSourceContextDto SourceContext { get; set; }
    public required StripeExeSelectedPaymentMethodDto SelectedPaymentMethod { get; set; }
}

public class StripeExeBillingContactDetailsDto
{
    public required string Email { get; set; }
}

public class StripeExeLineItemDto
{
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public decimal? UnitCost { get; init; }
    public int? Quantity { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
}

public class StripeExeSourceContextDto
{
    public required Guid Identifier { get; set; }
    public Guid? RepeatingTemplateId { get; set; }
    public required string Type { get; set; }
}

public class StripeExeSelectedPaymentMethodDto
{
    public required Guid PaymentGatewayId { get; set; }
    public required string PaymentMethodName { get; set; }
    public decimal? SurchargeAmount { get; set; }
}
