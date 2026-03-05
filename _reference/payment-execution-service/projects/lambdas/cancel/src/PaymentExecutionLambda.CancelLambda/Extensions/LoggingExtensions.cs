using Amazon.Lambda.SQSEvents;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using static PaymentExecution.Domain.Constants.HttpHeaders;

namespace PaymentExecutionLambda.CancelLambda.Extensions;

public static class LoggingExtensions
{
    public static IDisposable PushContextProperties(SQSEvent.SQSMessage message)
    {
        var enrichers = new ILogEventEnricher[]
        {
            new PropertyEnricher(XeroCorrelationId,
                message.GetCorrelationId()),
            new PropertyEnricher(XeroTenantId, message.GetTenantId() ?? "Unknown TenantId"),
            new PropertyEnricher("MessageId", message.MessageId)
        };

        return LogContext.Push(enrichers);
    }
}
