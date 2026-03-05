namespace PaymentExecution.PaymentRequestClient.Models;

public record LineItem
{
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public decimal? UnitCost { get; init; }
    public int? Quantity { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
}
