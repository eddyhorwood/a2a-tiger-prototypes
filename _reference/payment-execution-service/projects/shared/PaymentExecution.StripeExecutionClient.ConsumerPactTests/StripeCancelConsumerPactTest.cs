using System.Net;
using AutoFixture;
using PactNet;
using PaymentExecution.Common;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public abstract class StripeCancelConsumerPactTest
{
    private readonly Fixture _fixture = new();
    private readonly IPactBuilderV4 _pact;

    protected StripeCancelConsumerPactTest(IPactBuilderV4 pact)
    {
        _fixture.Customize<StripeExeCancelPaymentRequestDto>(comp => comp
            .With(x => x.PaymentRequestId, Guid.NewGuid())
            .With(x => x.TenantId, Guid.NewGuid())
            .With(x => x.CorrelationId, Guid.NewGuid())
            .With(x => x.CancellationReason, "abandoned"));

        _pact = pact;
    }

    [Fact]
    public async Task GivenCancellablePaymentRequest_WhenCorrectHeadersForCancelRequest_ThenReturnSuccess()
    {
        // Arrange
        var cancelPaymentRequest = _fixture.Create<StripeExeCancelPaymentRequestDto>();
        var paymentRequestId = cancelPaymentRequest.PaymentRequestId;
        var organisationId = cancelPaymentRequest.TenantId;
        var correlationId = cancelPaymentRequest.CorrelationId;
        var cancellationReason = cancelPaymentRequest.CancellationReason;

        var cancelEndpoint = Constants.StripeCancelEndpoint.Replace("{request-id}", paymentRequestId.ToString());

        _pact.UponReceiving("A POST request to the cancel stripe execution endpoint for a valid cancellable request")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithCancellableStatus, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Post, cancelEndpoint)
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeCancel)
                .WithJsonBody(new
                {
                    cancellationReason
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeCancel, organisationId, correlationId, null);

            var response = await apiClient.CancelStripeExecutionAsync(cancelPaymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task GivenSucceededPaymentRequest_WhenCancelRequest_ThenReturnsConflict()
    {
        // Arrange
        var cancelPaymentRequest = _fixture.Build<StripeExeCancelPaymentRequestDto>()
            .With(x => x.CancellationReason, "abandoned")
            .Create();
        var paymentRequestId = cancelPaymentRequest.PaymentRequestId;
        var organisationId = cancelPaymentRequest.TenantId;
        var correlationId = cancelPaymentRequest.CorrelationId;
        var cancellationReason = cancelPaymentRequest.CancellationReason;

        var cancelEndpoint = Constants.StripeCancelEndpoint.Replace("{request-id}", paymentRequestId.ToString());

        _pact.UponReceiving("A POST request to the cancel stripe execution endpoint for a payment that has already succeeded")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithSucceededStatus, StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Post, cancelEndpoint)
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeCancel)
                .WithJsonBody(new
                {
                    cancellationReason
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.Conflict);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeCancel, organisationId, correlationId, null);

            var response = await apiClient.CancelStripeExecutionAsync(cancelPaymentRequest);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        });
    }

    [Fact]
    public async Task GivenCancelledPaymentRequest_WhenCancelRequest_ThenReturnsSuccess()
    {
        // Arrange
        var cancelPaymentRequest = _fixture.Build<StripeExeCancelPaymentRequestDto>()
            .With(x => x.CancellationReason, "abandoned")
            .Create();
        var paymentRequestId = cancelPaymentRequest.PaymentRequestId;
        var organisationId = cancelPaymentRequest.TenantId;
        var correlationId = cancelPaymentRequest.CorrelationId;
        var cancellationReason = cancelPaymentRequest.CancellationReason;

        var cancelEndpoint = Constants.StripeCancelEndpoint.Replace("{request-id}", paymentRequestId.ToString());

        _pact.UponReceiving(
                "A POST request to the cancel stripe execution endpoint for a payment that has already been canceled")
            .Given(StripeExecutionServiceProviderState.APaymentRequestExistsWithCanceledStatus,
                StripeExecutionServiceProviderState.ParamDictionary(paymentRequestId))
            .WithRequest(HttpMethod.Post, cancelEndpoint)
            .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeCancel)
            .WithJsonBody(new { cancellationReason })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeCancel, organisationId, correlationId, null);

            var response = await apiClient.CancelStripeExecutionAsync(cancelPaymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task GivenNotExistentPaymentRequest_WhenCancelRequest_ThenReturnsNotFound()
    {
        // Arrange
        var cancelPaymentRequest = _fixture.Create<StripeExeCancelPaymentRequestDto>();
        var paymentRequestId = cancelPaymentRequest.PaymentRequestId;
        var organisationId = cancelPaymentRequest.TenantId;
        var correlationId = cancelPaymentRequest.CorrelationId;
        var cancellationReason = cancelPaymentRequest.CancellationReason;

        var cancelEndpoint = Constants.StripeCancelEndpoint.Replace("{request-id}", paymentRequestId.ToString());

        _pact.UponReceiving("A POST request to the cancel stripe execution endpoint for non-existent payment")
            .WithRequest(HttpMethod.Post, cancelEndpoint)
            .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
            .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
            .WithHeader(ExecutionConstants.Authorization, Constants.StripeCancel)
            .WithJsonBody(new { cancellationReason })
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeCancel, organisationId, correlationId, null);

            var response = await apiClient.CancelStripeExecutionAsync(cancelPaymentRequest);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }
}

