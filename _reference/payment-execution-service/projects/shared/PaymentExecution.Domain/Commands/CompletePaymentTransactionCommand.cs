using MediatR;
using PaymentExecution.Domain.Models;
using PaymentExecution.SqsIntegrationClient.Service;

namespace PaymentExecution.Domain.Commands;

public record CompletePaymentTransactionCommand : IRequest<Task>
{
    public required ExecutionQueueMessage Message { get; set; }
    public required string XeroTenantId { get; set; }
    public required string XeroCorrelationId { get; set; }
}

public class CompletePaymentTransactionCommandHandler(IExecutionQueueService executionQueueService) : IRequestHandler<CompletePaymentTransactionCommand,
    Task>
{
    public async Task<Task> Handle(CompletePaymentTransactionCommand request,
        CancellationToken cancellationToken)
    {
        await executionQueueService.SendMessageAsync(request.Message, request.XeroCorrelationId, request.XeroTenantId);
        return Task.CompletedTask;
    }
}
