namespace PaymentExecutionService.Models;

public record DchDeletePayload
{
    public required Guid IdToDelete { get; set; }
}
