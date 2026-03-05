using FluentResults;

namespace PaymentExecution.SqsIntegrationClient.Service;

public interface ICancelExecutionQueueService
{
    Task<Result> SendMessageAsync<T>(T message, int delaySeconds, string xeroCorrelationId, string xeroTenantId);
}
