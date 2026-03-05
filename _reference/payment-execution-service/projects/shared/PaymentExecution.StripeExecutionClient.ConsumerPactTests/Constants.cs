
namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public static class Constants
{
    public const string ProviderAccountId = "Provider-Account-Id";

    public const string StripeSubmit = "Submit";
    public const string StripeSubmitEndpoint = "/v1/payments/submit";

    public const string StripeCancel = "Cancel";
    public const string StripeCancelEndpoint = "/v1/payments/cancel/{request-id}";

    public const string StripeGetPaymentIntent = "Readstripepaymentintent";
    public const string StripeGetPaymentIntentEndpoint = "/v1/payments/payment-intent?paymentRequestId={payment-request-id}";
}
