using System.Net;
using System.Net.Mime;
using AutoFixture;
using FluentAssertions;
using PactNet;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using Xunit;

namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public class SucceedPaymentRequestConsumerPactTest
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IPactBuilderV4 _pact;

    public SucceedPaymentRequestConsumerPactTest(IPactBuilderV4 pact)
    {
        _fixture.Customize<SuccessPaymentRequest>(composer =>
            composer.With(p => p.FeeCurrency, "AUD"));
        _pact = pact;
    }

    [Fact]
    public async Task WhenCorrectHeadersForInProgressRequest_ThenReturnSuccessWithNoContent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var successRequest = _fixture.Create<SuccessPaymentRequest>();

        _pact.UponReceiving("A POST request to the execution-succeed payment request endpoint for a valid request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "executioninprogress"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/execution-succeed")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.ExecutionSucceedScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    successRequest.PaymentCompletionDateTime,
                    successRequest.Fee,
                    successRequest.FeeCurrency,
                    successRequest.PaymentProviderPaymentTransactionId,
                    successRequest.PaymentProviderPaymentReferenceId,
                    successRequest.PaymentProviderLastUpdatedAt
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.ExecutionSucceedScope, tenantId, correlationId);

            var result = await apiClient.ExecutionSucceedPaymentRequest(paymentRequestId, successRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task WhenPaymentRequestAlreadyInExecutionSuccess_ThenReturnSuccessWithNoContent()
    {
        // Arrange
        var correlationId = Guid.Parse("512332dc-9604-4f64-bee5-d0476638baf1");
        var paymentRequestId = Guid.NewGuid();
        var successRequest = _fixture.Create<SuccessPaymentRequest>();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving("A POST request to the success payment request endpoint for an already in progress request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "executionsuccess"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/execution-succeed")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.ExecutionSucceedScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    successRequest.PaymentCompletionDateTime,
                    successRequest.Fee,
                    successRequest.FeeCurrency,
                    successRequest.PaymentProviderPaymentTransactionId,
                    successRequest.PaymentProviderPaymentReferenceId,
                    successRequest.PaymentProviderLastUpdatedAt
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);

        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.ExecutionSucceedScope, tenantId, correlationId);

            var result = await apiClient.ExecutionSucceedPaymentRequest(paymentRequestId, successRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task WhenPaymentRequestNotFound_ThenReturnsNotFoundResponse()
    {
        // Arrange 
        var correlationId = Guid.Parse("1850d7b5-0d7e-4df4-a67d-442e28431745");
        var paymentRequestId = Guid.NewGuid();
        var successRequest = _fixture.Create<SuccessPaymentRequest>();
        var tenantId = Guid.NewGuid();

        _pact.UponReceiving($"A POST request to payment request service without a request populated in data store")
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/execution-succeed")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.ExecutionSucceedScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    successRequest.PaymentCompletionDateTime,
                    successRequest.Fee,
                    successRequest.FeeCurrency,
                    successRequest.PaymentProviderPaymentTransactionId,
                    successRequest.PaymentProviderPaymentReferenceId,
                    successRequest.PaymentProviderLastUpdatedAt
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NotFound);

        await _pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.ExecutionSucceedScope, tenantId, correlationId);

            var result = await apiClient.ExecutionSucceedPaymentRequest(paymentRequestId, successRequest, correlationId.ToString(), tenantId.ToString());

            //Assert
            result.IsFailed.Should().BeTrue();
        });
    }
}
