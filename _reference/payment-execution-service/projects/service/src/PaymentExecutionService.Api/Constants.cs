using System.Diagnostics.CodeAnalysis;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;

namespace PaymentExecutionService;

[ExcludeFromCodeCoverage]
public static class Constants
{
    public static class HttpClients
    {
        public const string XeroApi = nameof(XeroApi);
        public const string ThirdPartyApi = nameof(ThirdPartyApi);
    }
    public static class ServiceAuthorizationScopes
    {
        public const string ReadOnly =
            "xero_collecting-payments-execution_payment-execution-service.read";
        public const string Submit = "xero_collecting-payments-execution_payment-execution-service.submit";
        public const string Complete = "xero_collecting-payments-execution_payment-execution-service.complete";
        public const string DchDelete = "xero_collecting-payments-execution_payment-execution-service.dchdelete";
        public const string RequestCancel = "xero_collecting-payments-execution_payment-execution-service.requestcancel";
        public const string Cancel = "xero_collecting-payments-execution_payment-execution-service.cancel";
        public const string ReadProviderState =
            "xero_collecting-payments-execution_payment-execution-service.readproviderstate";
    }
    public static class ServiceAuthorizationPolicies
    {
        public const string ReadOnly = "ReadOnlyPolicy";
        public const string Submit = "SubmitPolicy";
        public const string DchDelete = "DchDeletePolicy";
        public const string Complete = "CompletePolicy";
        public const string RequestCancel = "RequestCancelPolicy";
        public const string Cancel = "CancelPolicy";
        public const string ReadProviderState = "ReadProviderStatePolicy";
    }

    public static class HttpHeaders
    {
        public const string ProviderAccountId = "Provider-Account-Id";
    }

    public static class RouteConstants
    {
        public const string SubmitStripePayment = "v{version:apiVersion}/payments/stripe/submit";
    }

    public static readonly CatalogueMetadata CatalogueMetadata = new(
        Name: "Payment Execution Service",
        Description:
        "The Execution Service is a central data store designed to manage and track execution transactions across multiple payment providers",
        ComponentUuid: "334db7HzPYMT119B9go9X9",
        // These values have been configured using Xero Gateway
        EnvironmentUrls: new Dictionary<string, string>
        {
            ["Local"] = "http://localhost:5000",
            ["Uat"] = "https://payment-execution.global.xero-uat.com/execution",
            ["Production"] = "https://payment-execution.global.xero.com/execution",
            ["Test"] = "https://payment-execution.global.xero-test.com/execution"
        },
        ApiType: XeroApiType.Product
    );
}
