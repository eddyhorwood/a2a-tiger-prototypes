namespace PaymentExecution.PaymentRequestClient.Models;

public static class ErrorMessage
{
    public const string SubmitPaymentRequestBadRequest = "Failed to submit payment request due to bad request";
    public const string ErrorDeserializingSubmitResponse = "Failed to deserialize response content";
}
