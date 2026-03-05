using System.ComponentModel.DataAnnotations;
using PaymentExecution.PaymentRequestClient.Models.Enums;

namespace PaymentExecution.PaymentRequestClient.Models;

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
