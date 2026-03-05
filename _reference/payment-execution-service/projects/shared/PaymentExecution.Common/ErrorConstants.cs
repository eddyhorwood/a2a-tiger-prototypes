namespace PaymentExecution.Common;

public static class ErrorConstants
{
    public static class ErrorCode
    {
        public const string ExecutionSubmitPaymentFailed = "execution_submit_payment_failed";
        public const string ExecutionSubmitError = "execution_submit_error";
        public const string ExecutionUnexpectedError = "execution_unexpected_error";
        public const string GenericExecutionError = "execution_error";
        public const string ExecutionCancellationError = "execution_cancellation_error";
        public const string ExecutionGetProviderStateError = "execution_get_provider_state_error";
    }

    public static class ErrorMessage
    {
        public const string SubmitPaymentRequestError = "Failed to submit payment request due to bad request";
        public const string SendMessageToCancelExecutionQueueError = "Failed to send message to cancel execution queue";
        public const string PaymentTransactionNotFoundError = "Payment transaction not found for the given payment request id";
        public const string CancelPaymentError = "Failed to cancel payment request at provider";
    }
}

public static class ErrorMetadataKey
{
    public const string ErrorType = "ErrorType";
    public const string ErrorCode = "ErrorCode";
    public const string ProviderErrorCode = "ProviderErrorCode";
    public const string HttpStatusCode = "HttpStatusCode";
}
