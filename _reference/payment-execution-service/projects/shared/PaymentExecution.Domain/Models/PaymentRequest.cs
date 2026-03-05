using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PaymentExecution.Domain.Models;

public class PaymentRequest
{
    public required Guid PaymentRequestId { get; init; }
    public required Guid OrganisationId { get; init; }
    public required DateTime PaymentDateUtc { get; set; }
    public DateTime? ScheduledDispatchDateUtc { get; set; }
    public required Guid ContactId { get; set; }
    public required RequestStatus Status { get; set; }
    public required BillingContactDetails BillingContactDetails { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string PaymentDescription { get; set; }
    public required SelectedPaymentMethod SelectedPaymentMethod { get; set; }

    [MaxLength(255)]
    public required List<LineItem> LineItems { get; set; }
    public required SourceContext SourceContext { get; set; }
    public required ExecutorType Executor { get; set; }

    [MaxLength(255)]
    public List<Receivable>? Receivables { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public required string MerchantReference { get; set; }
}

public record BillingContactDetails
{
    public required string Email { get; set; }
}

public record SelectedPaymentMethod
{
    public required Guid PaymentGatewayId { get; set; }
    public required string PaymentMethodName { get; set; }
    public decimal? SurchargeAmount { get; set; }
}

public record LineItem
{
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public decimal? UnitCost { get; init; }
    public int? Quantity { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
}

public record SourceContext
{
    public required Guid Identifier { get; set; }
    public Guid? RepeatingTemplateId { get; set; }
    public required string Type { get; set; }
}

public record Receivable
{
    public required Guid Identifier { get; set; }
    public required ReceivableType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    created,
    scheduled,
    awaitingexecution,
    executioninprogress,
    executionsuccess,
    cancelled,
    failed,
    success
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceivableType
{
    invoice
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExecutorType
{
    webpay,
    paymentexecution
}
