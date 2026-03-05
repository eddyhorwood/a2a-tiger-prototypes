using System.Net;
using System.Net.Mime;
using AutoFixture;
using FluentAssertions;
using PactNet;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using Xunit;

namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public class FailPaymentRequestConsumerTest
{
    private readonly Fixture _fixture = new();
    private readonly IPactBuilderV4 _pact;
    protected FailPaymentRequestConsumerTest(IPactBuilderV4 pact)
    {
        _fixture.Customize<FailurePaymentRequest>(composer =>
            composer.With(p => p.FeeCurrency, "AUD"));
        _pact = pact;
    }

    [Fact]
    public async Task WhenCorrectHeadersForInProgressRequest_ThenReturnSuccessWithNoContent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var failureRequest = _fixture.Create<FailurePaymentRequest>();

        _pact.UponReceiving("A POST request to the fail payment request endpoint for a valid request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "executioninprogress"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/fail")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.FailScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    failureRequest.PaymentCompletionDateTime,
                    failureRequest.Fee,
                    failureRequest.FeeCurrency,
                    failureRequest.PaymentProviderPaymentTransactionId,
                    failureRequest.PaymentProviderPaymentReferenceId,
                    failureRequest.FailureDetails
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.FailScope, tenantId, correlationId);

            var result = await apiClient.FailPaymentRequest(paymentRequestId, failureRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task WhenPaymentRequestAlreadyInFailedStatus_ThenReturnSuccessWithNoContent()
    {
        // Arrange
        var correlationId = Guid.Parse("512332dc-9604-4f64-bee5-d0476638baf1");
        var paymentRequestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var failureRequest = _fixture.Create<FailurePaymentRequest>();

        _pact.UponReceiving("A POST request to the fail payment request endpoint for an already in progress request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "failed"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/fail")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.FailScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    failureRequest.PaymentCompletionDateTime,
                    failureRequest.Fee,
                    failureRequest.FeeCurrency,
                    failureRequest.PaymentProviderPaymentTransactionId,
                    failureRequest.PaymentProviderPaymentReferenceId,
                    failureRequest.FailureDetails
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);


        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.FailScope, tenantId, correlationId);

            var result = await apiClient.FailPaymentRequest(paymentRequestId, failureRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task WhenPaymentRequestNotFound_ThenReturnsNotFoundResponse()
    {
        var correlationId = Guid.Parse("1850d7b5-0d7e-4df4-a67d-442e28431745");
        var paymentRequestId = Guid.NewGuid();
        var failureRequest = _fixture.Create<FailurePaymentRequest>();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A POST request to payment request service without a request populated in data store")
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/fail")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.FailScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    failureRequest.PaymentCompletionDateTime,
                    failureRequest.Fee,
                    failureRequest.FeeCurrency,
                    failureRequest.PaymentProviderPaymentTransactionId,
                    failureRequest.PaymentProviderPaymentReferenceId,
                    failureRequest.FailureDetails
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NotFound);

        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.FailScope, tenantId, correlationId);

            var result = await apiClient.FailPaymentRequest(paymentRequestId, failureRequest, correlationId.ToString(), tenantId.ToString());

            result.IsFailed.Should().BeTrue();
        });
    }
}
