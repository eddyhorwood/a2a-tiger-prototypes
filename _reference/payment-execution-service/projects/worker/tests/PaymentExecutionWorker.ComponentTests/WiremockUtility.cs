namespace PaymentExecutionWorker.ComponentTests;

public static class WiremockUtility
{
    public class WiremockRequest
    {
        public required RequestObject Request { get; set; }
    }

    public class RequestObject
    {
        public required string Url { get; set; }
    }
}
