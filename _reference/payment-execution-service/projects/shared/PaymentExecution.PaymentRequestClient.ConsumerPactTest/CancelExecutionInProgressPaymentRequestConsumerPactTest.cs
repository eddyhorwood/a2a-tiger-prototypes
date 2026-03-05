using System.Net;
using System.Net.Mime;
using AutoFixture;
using FluentAssertions;
using PactNet;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using Xunit;
namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public class CancelExecutionInProgressPaymentRequestConsumerTests(IPactBuilderV4 pact)
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task GivenCorrectHeadersForInProgressRequest_WhenCancelExecutionInProgress_ThenReturnsSuccessWithNoContent()

    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var cancelRequest = _fixture.Create<CancelPaymentRequest>();

        pact.UponReceiving("A POST request to the cancel execution in progress payment request endpoint for a valid request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "executioninprogress"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/cancel-execution-in-progress")
                .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.CancelExecutionInProgressScope)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
                .WithJsonBody(new
                {
                    cancelRequest.PaymentProviderPaymentTransactionId,
                    cancelRequest.PaymentProviderLastUpdatedAt,
                    cancelRequest.CancellationReason
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.NoContent);

        await pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.CancelExecutionInProgressScope, tenantId, correlationId);

            var result = await apiClient.CancelPaymentRequest(paymentRequestId, cancelRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GivenPaymentRequestAlreadyCancelled_WhenCancelExecutionInProgress_ThenReturnSuccessWithNoContent()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var cancelRequest = _fixture.Create<CancelPaymentRequest>();

        pact.UponReceiving("A POST request to the cancel execution in progress payment request endpoint for an already cancelled payment request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "cancelled"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/cancel-execution-in-progress")
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.CancelExecutionInProgressScope)
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
            .WithJsonBody(new
            {
                cancelRequest.PaymentProviderPaymentTransactionId,
                cancelRequest.PaymentProviderLastUpdatedAt,
                cancelRequest.CancellationReason
            })
            .WillRespond()
            .WithStatus(HttpStatusCode.NoContent);

        await pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.CancelExecutionInProgressScope, tenantId, correlationId);

            var result = await apiClient.CancelPaymentRequest(paymentRequestId, cancelRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsSuccess.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GivenPaymentRequestDoesNotExist_WhenCancelExecutionInProgress_ThenReturnNotFound()
    {
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var cancelRequest = _fixture.Create<CancelPaymentRequest>();
        var tenantId = Guid.NewGuid();

        pact.UponReceiving("A POST request to payment request service without a request populated in data store")
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/cancel-execution-in-progress")
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.CancelExecutionInProgressScope)
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
            .WithJsonBody(new
            {
                cancelRequest.PaymentProviderPaymentTransactionId,
                cancelRequest.PaymentProviderLastUpdatedAt,
                cancelRequest.CancellationReason
            })
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await pact.VerifyAsync(async ctx =>
        {
            // Act
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.CancelExecutionInProgressScope, tenantId, correlationId);

            var result = await apiClient.CancelPaymentRequest(paymentRequestId, cancelRequest, correlationId.ToString(), tenantId.ToString());

            result.IsFailed.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GivenPaymentRequestAlreadySucceeded_WhenCancelExecutionInProgress_ThenReturnsBadRequest()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var cancelRequest = _fixture.Create<CancelPaymentRequest>();

        pact.UponReceiving("A POST request to the cancel execution in progress payment request endpoint for an already succeeded payment request")
            .Given(PaymentRequestServiceProviderState.RequestInProgress, PaymentRequestServiceProviderState.ParamDictionary(paymentRequestId, "success"))
            .WithRequest(HttpMethod.Post, $"/v1/payment-requests/{paymentRequestId}/cancel-execution-in-progress")
            .WithHeader(ExecutionConstants.XeroTenantId, tenantId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.CancelExecutionInProgressScope)
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.ContentType, MediaTypeNames.Application.Json)
            .WithJsonBody(new
            {
                cancelRequest.PaymentProviderPaymentTransactionId,
                cancelRequest.PaymentProviderLastUpdatedAt,
                cancelRequest.CancellationReason
            })
            .WillRespond()
            .WithStatus(HttpStatusCode.BadRequest);

        await pact.VerifyAsync(async ctx =>
        {
            var apiClient = PaymentRequestConsumerPactTest.CreatePaymentRequestClient(ctx, Constants.CancelExecutionInProgressScope, tenantId, correlationId);

            var result = await apiClient.CancelPaymentRequest(paymentRequestId, cancelRequest, correlationId.ToString(), tenantId.ToString());

            // Assert
            result.IsFailed.Should().BeTrue();
        });
    }
}
