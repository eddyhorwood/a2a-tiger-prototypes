namespace PaymentExecution.Common;

public static class ExecutionConstants
{
    public const string XeroCorrelationId = "Xero-Correlation-Id";
    public const string XeroTenantId = "Xero-Tenant-Id";
    public const string Authorization = "Authorization";
    public const string ContentType = "Content-Type";
    public const string ApplicationJsonMediaType = "application/json";

    public static class ErrorMessages
    {
        public const string UnexpectedMessageProcessingError = "An unexpected error occurred while processing messages in the execution queue";
    }

    public static class InfoMessages
    {
        public const string WorkerOperationCancelledDueToShutdown = "Worker operation was cancelled due to shutdown request";
        public const string WorkerDelayCancelledDueToShutdown = "Worker delay was cancelled due to shutdown request";
    }

    public static class FeatureFlags
    {
        public static readonly FeatureFlagDefinition<bool> PaymentExecutionServiceEnabled =
            new("payment-execution-service-enabled", false);

        public static readonly FeatureFlagDefinition<bool> SendMessageToCancelExecutionQueue =
            new("inpay-22618-send-message-to-cancel-execution-queue", true);

        public static readonly FeatureFlagDefinition<bool> EnableProviderCancellation =
            new("enable-payment-request-cancellation-with-provider", false);
    }

    public static class NewRelicConstants
    {
        private const string TraceParent = "traceparent";
        private const string NewRelicPascal = "NewRelic";
        private const string NewRelicUpper = "NEWRELIC";
        private const string NewRelicLower = "newrelic";
        private const string XSynethetics = "X-NewRelic-Synthetics";

        public static readonly string[] DistributedTracingHeaders = [
            TraceParent,
            NewRelicPascal,
            NewRelicUpper,
            NewRelicLower,
            XSynethetics
        ];
    }
}

public record FeatureFlagDefinition<T>(string Name, T DefaultValue);
