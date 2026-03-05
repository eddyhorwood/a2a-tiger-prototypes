using System.Net;
using PactNet;
using PactNet.Matchers;
using PaymentExecution.Common;

namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public abstract class StripePaymentIntentConsumerPactTest
{
    private readonly IPactBuilderV4 _pact;

    private static class NextActionType
    {
        public const string VerifyWithMicrodeposits = "verify_with_microdeposits";
        public const string DisplayBankTransferInstructions = "display_bank_transfer_instructions";
        public const string RedirectToUrl = "redirect_to_url";
        public const string PaytoAwaitAuthorization = "payto_await_authorization";
    }

    protected StripePaymentIntentConsumerPactTest(IPactBuilderV4 pact)
    {
        _pact = pact;
    }

    [Fact]
    public async Task
        GivenPaymentWithMicrodepositVerificationNextAction_WhenCallingPaymentIntentRequest_ThenVerifyWithMicrodepositsActionIsReturned()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A GET request to the payment intent endpoint for a payment with microdeposit verification next action")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithPaymentIntentNextActionVerifyWithMicrodeposits, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Get, "/v1/payments/payment-intent")
                .WithQuery("paymentRequestId", paymentRequestId.ToString())
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeGetPaymentIntent)
            .WillRespond()
                .WithJsonBody(CreateExpectedPaymentIntentResponseBody(NextActionType.VerifyWithMicrodeposits))
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeGetPaymentIntent, tenantId, correlationId, null);

            var response = await apiClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }
    [Fact]
    public async Task GivenPaymentWithBankTransferInstructionsNextAction_WhenCallingPaymentIntentRequest_ThenDisplayBankTransferInstructionsActionIsReturned()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A GET request to the payment intent endpoint for a payment with display bank transfer instructions next action")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithPaymentIntentNextActionDisplayBankTransferInstructions, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Get, "/v1/payments/payment-intent")
            .WithQuery("paymentRequestId", paymentRequestId.ToString())
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeGetPaymentIntent)
            .WillRespond()
            .WithJsonBody(CreateExpectedPaymentIntentResponseBody(NextActionType.DisplayBankTransferInstructions))
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeGetPaymentIntent, tenantId, correlationId, null);

            var response = await apiClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task
        GivenPaymentWithRedirectToUrlNextAction_WhenCallingPaymentIntentRequest_ThenRedirectToUrlActionIsReturned()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A GET request to the payment intent endpoint for a payment with redirect to url next action")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithPaymentIntentNextActionRedirectToUrl, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Get, "/v1/payments/payment-intent")
            .WithQuery("paymentRequestId", paymentRequestId.ToString())
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeGetPaymentIntent)
            .WillRespond()
            .WithJsonBody(CreateExpectedPaymentIntentResponseBody(NextActionType.RedirectToUrl))
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeGetPaymentIntent, tenantId, correlationId, null);

            var response = await apiClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task
        GivenPaymentWithPayToAwaitAuthorizationNextAction_WhenCallingPaymentIntentRequest_ThenPayToAwaitAuthorizationActionIsReturned()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A GET request to the payment intent endpoint for a payment with PayTo await authorization next action")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithPaymentIntentNextActionPayToAwaitAuthorization, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Get, "/v1/payments/payment-intent")
            .WithQuery("paymentRequestId", paymentRequestId.ToString())
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeGetPaymentIntent)
            .WillRespond()
            .WithJsonBody(CreateExpectedPaymentIntentResponseBody(NextActionType.PaytoAwaitAuthorization))
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeGetPaymentIntent, tenantId, correlationId, null);

            var response = await apiClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task
        GivenFailedPaymentIntent_WhenCallingPaymentIntentRequest_ThenLastPaymentErrorCodeIsReturned()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A GET request to the payment intent endpoint for a failed payment intent with LastPaymentError")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithAFailedPaymentIntent, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Get, "/v1/payments/payment-intent")
            .WithQuery("paymentRequestId", paymentRequestId.ToString())
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeGetPaymentIntent)
            .WillRespond()
            .WithJsonBody(CreateExpectedPaymentIntentResponseBody(includeLastPaymentError: true))
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeGetPaymentIntent, tenantId, correlationId, null);

            var response = await apiClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    /// <summary>
    /// Creates the expected payment intent response body structure for Pact contract testing.
    /// Dynamically constructs the response based on the next action type or failed payment scenario,
    /// including appropriate next action details, payment method type, and last payment error for validation.
    /// </summary>
    /// <param name="nextActionType">The type of next action to include in the response (e.g., verify_with_microdeposits, display_bank_transfer_instructions, redirect_to_url, payto_await_authorization). Can be null for failed payments.</param>
    /// <param name="includeLastPaymentError">If true, includes lastPaymentError in the response instead of nextAction</param>
    /// <returns>An anonymous object representing the expected payment intent response structure</returns>
    private static object CreateExpectedPaymentIntentResponseBody(string? nextActionType = null, bool includeLastPaymentError = false)
    {
        if (includeLastPaymentError)
        {
            // Failed payment scenario
            return new
            {
                id = Match.Regex("pi_12345", "pi_[a-zA-Z0-9_]+"),
                amount = Match.Integer(5000),
                currency = Match.Regex("usd", "^[a-zA-Z]{3}$"),
                status = "requires_payment_method",
                lastPaymentError = new
                {
                    code = Match.Type("card_declined"),
                    declineCode = Match.Type("generic_decline"),
                    paymentMethodType = Match.Type("card")
                }
            };
        }

        // Success/pending scenario with next action
        object nextAction = nextActionType switch
        {
            NextActionType.VerifyWithMicrodeposits => new
            {
                type = "verify_with_microdeposits",
                verifyWithMicrodeposits = new { hostedVerificationUrl = Match.Include("microdeposit") }
            },
            NextActionType.DisplayBankTransferInstructions => new
            {
                type = "display_bank_transfer_instructions",
                displayBankTransferInstructions = new
                {
                    amountRemaining = Match.Integer(5000),
                    currency = Match.Regex("usd", "^[a-zA-Z]{3}$"),
                    reference = Match.Regex("M92KRG2BNH4K", "^[A-Z0-9]{12}$"),
                    financialAddresses = Match.Type(new
                    object[]
                    {
                        new {
                            type = Match.Regex("aba", "[a-zA-Z]*"),
                            aba = new
                            {
                                accountNumber = Match.Regex("11119976219408796", "[0-9]{17}"),
                                bankName = Match.Regex("STRIPE TEST BANK", "[a-zA-Z ]*"),
                                routingNumber = Match.Regex("999999999", "[0-9]{9}")
                            }
                        },
                        new
                        {
                            type = Match.Regex("swift", "[a-zA-Z]*"),
                            swift = new
                            {
                                swiftCode = Match.Regex("TESTUS99XXX", "^[A-Z0-9]{8,11}$")
                            }
                        }
                    }),
                }
            },
            NextActionType.RedirectToUrl => new
            {
                type = "redirect_to_url",
                redirectToUrl = new { url = Match.Include("redirects") }
            },
            NextActionType.PaytoAwaitAuthorization => new { type = "payto_await_authorization" },
            _ => throw new ArgumentException($"Unsupported next action type: {nextActionType}")
        };

        object paymentMethod = nextActionType switch
        {
            NextActionType.VerifyWithMicrodeposits => new { type = "us_bank_account" },
            NextActionType.DisplayBankTransferInstructions => new { type = "customer_balance" },
            NextActionType.RedirectToUrl => new { type = "zip" },
            NextActionType.PaytoAwaitAuthorization => new { type = "payto" },
            _ => throw new ArgumentException($"Unsupported next action type: {nextActionType}")
        };

        return new
        {
            id = Match.Regex("pi_12345", "pi_[a-zA-Z0-9_]+"),
            amount = Match.Integer(5000),
            currency = Match.Regex("usd", "^[a-zA-Z]{3}$"),
            status = "requires_action",
            nextAction,
            paymentMethod
        };
    }
}


