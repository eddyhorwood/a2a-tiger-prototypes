namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public static class StripeExecutionServiceProviderState
{
    public const string APaymentRequestExistsWithCancellableStatus = "A payment request exists with cancellable status";
    public const string APaymentRequestExistsWithSucceededStatus = "A payment request exists with succeeded status";
    public const string APaymentRequestExistsWithCanceledStatus = "A payment request exists with canceled status";
    public const string APaymentRequestExistsWithPaymentIntentNextActionDisplayBankTransferInstructions = "A payment request exists with a payment intent that has next action to display bank transfer instructions";
    public const string APaymentRequestExistsWithPaymentIntentNextActionRedirectToUrl = "A payment request exists with a payment intent that has next action to redirect to url";
    public const string APaymentRequestExistsWithPaymentIntentNextActionVerifyWithMicrodeposits = "A payment request exists with a payment intent that has next action to verify with microdeposits";
    public const string APaymentRequestExistsWithPaymentIntentNextActionPayToAwaitAuthorization = "A payment request exists with a payment intent that has next action to pay to await authorization";
    public const string APaymentRequestExistsWithAFailedPaymentIntent = "A payment request exists with a failed payment intent";


    public static Dictionary<string, string> ParamDictionary(Guid paymentRequestId)
    {
        return new Dictionary<string, string>
        {
            { "payment-request-id", paymentRequestId.ToString() }
        };
    }
}
