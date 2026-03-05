namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public static class Constants
{
    public const string FailScope = "FailScope";
    public const string SubmitScope = "SubmitScope";
    public const string ExecutionSucceedScope = "ExecutionSucceedScope";
    public const string CancelExecutionInProgressScope = "CancelExecutionInProgressScope";

    public const string SubmitEndpoint = "v1/payment-requests/{request-id}/submit";
    public const string ExecutionSucceedEndpoint = "v1/payment-requests/{request-id}/execution-succeed";
    public const string FailureEndpoint = "v1/payment-requests/{request-id}/fail";
    public const string GetPaymentRequestByIdEndpoint = "v1/payment-requests/{request-id}";
    public const string CancelExecutionInProgressEndpoint = "v1/payment-requests/{request-id}/cancel-execution-in-progress";
}
