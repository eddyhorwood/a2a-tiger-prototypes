namespace PaymentExecutionService.ComponentTests;

public static class Constants
{
    public static class UserInformation
    {
        public static readonly string UserIdAdmin = "0c89fed3-4690-44c5-ac1c-7e9e7322dfae";
        public static readonly string TenantIdOrg = "11111111-2222-3333-4444-555555555555";
    }

    public static class Endpoints
    {
        public static readonly string PaymentTransactionsRoot = "v1/payments";
        public static readonly string SubmitStripePayment = $"v1/payments/stripe/submit";
        public static readonly string GetPaymentTransaction = $"v1/payments?paymentRequestId=";
    }

    public static class StripeExeWireMockGuids
    {
        public const string StripeExeSuccessResponseTenantId = "55159241-3379-4884-8b38-bd730fe8ea9b";
        public const string StripeExeNonTransientErrorResponseTenantId = "c7dd73bf-ed5d-4970-b0e4-801a0ec44def";
        public const string StripeExeTransientErrorResponseTenantId = "68fbe448-691e-45d2-a328-df7ac3db6c90";
    }
}
