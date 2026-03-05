namespace PaymentExecution.Domain;

public static class Constants
{
    public static class ValidExecutionQueueStatuses
    {
        public const string Failed =
            "Failed";

        public const string Succeeded =
            "Succeeded";
    }

    public static class HttpHeaders
    {
        public const string XeroCorrelationId = "Xero-Correlation-Id";
        public const string XeroTenantId = "Xero-Tenant-Id";
    }
}
