using System.Globalization;
using System.Net;
using AutoFixture;
using FluentResults;
using PactNet;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models;
using Xunit;
using Match = PactNet.Matchers.Match;

namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public abstract class SubmitPaymentRequestConsumerPactTest
{
    private readonly IFixture _fixture = new Fixture();
    private readonly PaymentRequest _mockedSubmitResponse;
    private readonly IPactBuilderV4 _pact;

    protected SubmitPaymentRequestConsumerPactTest(IPactBuilderV4 pact)
    {
        _pact = pact;
        _mockedSubmitResponse = _fixture.Create<PaymentRequest>();
    }

    [Fact]
    public async Task WhenCorrectHeadersForInProgressRequest_ThenReturnSuccessWithPaymentRequest()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();

        _pact.UponReceiving("A POST request to the submit payment request endpoint for a valid request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "awaitingexecution"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/submit")
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.SubmitScope)
            .WillRespond()
                .WithJsonBody(CreateValidPaymentRequestSubmitResponseBody(paymentRequestId))
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.SubmitScope, organisationId, correlationId);

            var response = await apiClient.SubmitPaymentRequest(paymentRequestId);

            // Assert
            AssertResponseValuesMatchProviderState(response, paymentRequestId);
        });
    }

    [Fact]
    public async Task WhenPaymentRequestAlreadyInExecutionInProgress_ThenReturnSuccessWithPaymentRequest()
    {
        // Arrange
        var organisationId = Guid.Parse("1850d7b5-0d7e-4df4-a67d-442e28431745");
        var correlationId = Guid.Parse("512332dc-9604-4f64-bee5-d0476638baf1");
        var paymentRequestId = Guid.NewGuid();

        _pact.UponReceiving("A POST request to the submit payment request endpoint for an already in progress request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "executioninprogress"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/submit")
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.SubmitScope)
            .WillRespond()
                .WithJsonBody(CreateValidPaymentRequestSubmitResponseBody(paymentRequestId))
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.SubmitScope, organisationId, correlationId);

            var response = await apiClient.SubmitPaymentRequest(paymentRequestId);

            // Assert
            AssertResponseValuesMatchProviderState(response, paymentRequestId);
        });
    }

    [Fact]
    public async Task WhenPaymentRequestNotFound_ThenReturnsNotFoundResponse()
    {
        // Arrange
        var organisationId = Guid.Parse("1850d7b5-0d7e-4df4-a67d-442e28431745");
        var correlationId = Guid.Parse("1850d7b5-0d7e-4df4-a67d-442e28431745");
        var paymentRequestId = Guid.NewGuid();

        _pact.UponReceiving($"A POST request to payment request service without a request populated in data store")
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/submit")
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.SubmitScope)
            .WillRespond()
                .WithStatus(HttpStatusCode.NotFound);

        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.SubmitScope, organisationId, correlationId);

            var result = await apiClient.SubmitPaymentRequest(paymentRequestId);
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorMessage.SubmitPaymentRequestBadRequest, result.GetFirstErrorMessage());
        });
    }

    private object CreateValidPaymentRequestSubmitResponseBody(Guid paymentRequestId)
    {
        return new
        {
            paymentRequestId,
            organisationId = Match.Regex(_mockedSubmitResponse.OrganisationId.ToString(), "[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}"),
            paymentDateUtc = Match.Regex(_mockedSubmitResponse.PaymentDateUtc.ToString("O", CultureInfo.InvariantCulture), ".*"),
            contactId = Match.Regex(_mockedSubmitResponse.ContactId.ToString(), "[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}"),
            status = Match.Regex(_mockedSubmitResponse.Status.ToString(), "\\w+"),
            billingContactDetails = new
            {
                email = Match.Regex(_mockedSubmitResponse.BillingContactDetails.Email, ".+@.+")
            },
            amount = Match.Decimal(_mockedSubmitResponse.Amount),
            currency = Match.Regex(_mockedSubmitResponse.Currency, "^[a-zA-Z]{3}$"),
            paymentDescription = Match.Regex(_mockedSubmitResponse.PaymentDescription, "\\w+"),
            selectedPaymentMethod = new
            {
                paymentGatewayId = Match.Regex(_mockedSubmitResponse.SelectedPaymentMethod.PaymentGatewayId.ToString(), "[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}"),
                paymentMethodName = Match.Regex(_mockedSubmitResponse.SelectedPaymentMethod.PaymentMethodName, ".*")
            },
            lineItems = Match.MinMaxType(new
            {
                unitCost = Match.Decimal(_mockedSubmitResponse.LineItems[0].UnitCost ?? 0m),
                quantity = Match.Integer(_mockedSubmitResponse.LineItems[0].Quantity ?? 0)
            }, 0, 999999),
            sourceContext = new
            {
                identifier = Match.Regex(_mockedSubmitResponse.SourceContext.Identifier.ToString(), "[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}"),
                type = Match.Regex(_mockedSubmitResponse.SourceContext.Type, "[a-zA-Z]*")
            },
            executor = Match.Regex(_mockedSubmitResponse.Executor.ToString(), "[a-zA-Z]*"),
            receivables = Match.MinMaxType(new
            {
                identifier = Match.Regex(_mockedSubmitResponse.Receivables?[0].Identifier.ToString(), "[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}"),
                type = Match.Regex(_mockedSubmitResponse.Receivables?[0].Type.ToString(), "[a-zA-Z]*")
            }, 0, 9999999),
            merchantReference = Match.Regex(_mockedSubmitResponse.MerchantReference, "\\w+")
        };
    }

    private void AssertResponseValuesMatchProviderState(Result<PaymentRequest> result, Guid paymentRequestId)
    {
        var resultPaymentRequest = result.Value;
        Assert.Equal(paymentRequestId, resultPaymentRequest.PaymentRequestId);
        Assert.Equal(_mockedSubmitResponse.OrganisationId, resultPaymentRequest.OrganisationId);
        Assert.Equal(_mockedSubmitResponse.PaymentDateUtc, resultPaymentRequest.PaymentDateUtc);
        Assert.Equal(_mockedSubmitResponse.ContactId, resultPaymentRequest.ContactId);
        Assert.Equal(_mockedSubmitResponse.Status, resultPaymentRequest.Status);
        Assert.Equal(_mockedSubmitResponse.BillingContactDetails.Email, resultPaymentRequest.BillingContactDetails.Email);
        Assert.Equal(_mockedSubmitResponse.Amount, resultPaymentRequest.Amount);
        Assert.Equal(_mockedSubmitResponse.Currency, resultPaymentRequest.Currency);
        Assert.Equal(_mockedSubmitResponse.PaymentDescription, resultPaymentRequest.PaymentDescription);
        Assert.Equal(_mockedSubmitResponse.SelectedPaymentMethod.PaymentMethodName, resultPaymentRequest.SelectedPaymentMethod.PaymentMethodName);
        Assert.Equal(_mockedSubmitResponse.SelectedPaymentMethod.PaymentGatewayId, resultPaymentRequest.SelectedPaymentMethod.PaymentGatewayId);
        if (_mockedSubmitResponse.LineItems.Count > 0 && resultPaymentRequest.LineItems.Count > 0)
        {
            Assert.Equal(_mockedSubmitResponse.LineItems[0].UnitCost, resultPaymentRequest.LineItems[0].UnitCost);
            Assert.Equal(_mockedSubmitResponse.LineItems[0].Quantity, resultPaymentRequest.LineItems[0].Quantity);
        }
        Assert.Equal(_mockedSubmitResponse.SourceContext.Identifier, resultPaymentRequest.SourceContext.Identifier);
        Assert.Equal(_mockedSubmitResponse.SourceContext.Type, resultPaymentRequest.SourceContext.Type);
        Assert.Equal(_mockedSubmitResponse.Executor, resultPaymentRequest.Executor);
        Assert.Equal(_mockedSubmitResponse.Receivables?[0].Identifier, resultPaymentRequest.Receivables?[0].Identifier);
        Assert.Equal(_mockedSubmitResponse.Receivables?[0].Type, resultPaymentRequest.Receivables?[0].Type);
        Assert.Equal(_mockedSubmitResponse.MerchantReference, resultPaymentRequest.MerchantReference);
    }
}
