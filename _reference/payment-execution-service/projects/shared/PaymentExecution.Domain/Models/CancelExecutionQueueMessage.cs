using System.Diagnostics.CodeAnalysis;

namespace PaymentExecution.Domain.Models;

[ExcludeFromCodeCoverage]
public record CancelExecutionQueueMessage
{
    public required Guid PaymentRequestId { get; set; }
    public required CancellationReason CancellationReason { get; set; }
    public required ProviderType ProviderType { get; set; }
}
