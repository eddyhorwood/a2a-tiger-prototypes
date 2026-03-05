using System.ComponentModel.DataAnnotations;

namespace PaymentExecutionService.Models;

public class RequestCancelPayload
{
    [Required]
    public required string CancellationReason { get; set; }
}
