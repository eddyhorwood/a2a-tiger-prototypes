namespace PaymentExecutionLambda.CancelLambda.ComponentTests;

public class Constants
{
    public static class StripeExeMockTenantIds
    {
        public static readonly Guid GetProviderStatusRequiresAction = Guid.Parse("55159241-3379-4884-8b38-bd730fe8ea9b");
        public static readonly Guid GetProviderStatusCancellable = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        public static readonly Guid GetProviderStatusTransientError = Guid.Parse("68fbe448-691e-45d2-a328-df7ac3db6c90");
        public static readonly Guid GetProviderStatusNonTransientError = Guid.Parse("c7dd73bf-ed5d-4970-b0e4-801a0ec44def");
    }
    public static class StripeExeMockRequestIds
    {
        public static readonly Guid CancelTransientError = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly Guid Cancel404NotFound = Guid.Parse("00000000-0000-0000-0000-000000000002");
        public static readonly Guid Cancel4xxClientError = Guid.Parse("00000000-0000-0000-0000-000000000003");
    }
}
