using System.Net;
using AutoFixture;
using PactNet;
using PactNet.Matchers;
using PaymentExecution.Common;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public abstract class StripeSubmitConsumerPactTest
{
    private readonly Fixture _fixture = new();
    private readonly IPactBuilderV4 _pact;

    protected StripeSubmitConsumerPactTest(IPactBuilderV4 pact)
    {
        _fixture.Customize<StripeExePaymentRequestDto>(comp => comp
            .With(x => x.PaymentRequestId, Guid.NewGuid())
            .With(x => x.OrganisationId, Guid.NewGuid())
            .With(x => x.Amount, Random.Shared.Next(1, 10000) / 100.0m)
            .With(x => x.Currency, "aud")
            .With(x => x.Executor, "paymentexecution"));

        _pact = pact;
    }

    [Fact]
    public async Task WhenCorrectHeadersForSubmitRequest_ThenReturnSuccessWithExpectedPayload()
    {
        // Arrange
        var mockedPaymentRequest = _fixture.Create<StripeExePaymentRequestDto>();
        var organisationId = mockedPaymentRequest.OrganisationId;
        var correlationId = Guid.NewGuid();
        var providerAccountId = GenerateMockStripeAccountId();

        var submitPaymentRequest = new StripeExeSubmitPaymentRequestDto()
        {
            PaymentMethodId = "some-payment-method-id",
            PaymentRequest = mockedPaymentRequest,
            PaymentMethodsMadeAvailable = ["card"]
        };

        _pact.UponReceiving("A POST request to the submit stripe execution endpoint for a valid request")
            .WithRequest(HttpMethod.Post, Constants.StripeSubmitEndpoint)
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(Constants.ProviderAccountId, providerAccountId)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeSubmit)
                .WithJsonBody(submitPaymentRequest)
            .WillRespond()
                .WithJsonBody(new
                {
                    clientSecret = Match.Regex("pi_12345_secret_abcd1234", @"^[a-zA-Z0-9_-]+"),
                    paymentIntentId = Match.Regex("pi_12345", @"^[a-zA-Z0-9_-]+"),
                    providerServiceId = Match.Regex(organisationId.ToString(), @"^[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}$")
                })
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeSubmit, organisationId, correlationId, providerAccountId);

            var response = await apiClient.SubmitStripeExecutionAsync(submitPaymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task WhenPayloadDoesNotContainPaymentMethodsMadeAvailableInValidSubmitRequest_ThenReturnSuccessWithExpectedPayload()
    {
        // Arrange
        var mockedPaymentRequest = _fixture.Create<StripeExePaymentRequestDto>();
        var organisationId = mockedPaymentRequest.OrganisationId;
        var correlationId = Guid.NewGuid();
        var providerAccountId = GenerateMockStripeAccountId();

        var submitPaymentRequest = new StripeExeSubmitPaymentRequestDto()
        {
            PaymentMethodId = "some-payment-method-id",
            PaymentRequest = mockedPaymentRequest,
        };

        _pact.UponReceiving("A POST request to the submit stripe execution endpoint for a valid request without PaymentMethodsMadeAvailable")
            .WithRequest(HttpMethod.Post, Constants.StripeSubmitEndpoint)
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(Constants.ProviderAccountId, providerAccountId)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeSubmit)
                .WithJsonBody(submitPaymentRequest)
            .WillRespond()
                .WithJsonBody(new
                {
                    clientSecret = Match.Regex("pi_12345_secret_abcd1234", @"^[a-zA-Z0-9_-]+"),
                    paymentIntentId = Match.Regex("pi_12345", @"^[a-zA-Z0-9_-]+"),
                    providerServiceId = Match.Regex(organisationId.ToString(), @"^[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}$")
                })
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeSubmit, organisationId, correlationId, providerAccountId);

            var response = await apiClient.SubmitStripeExecutionAsync(submitPaymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async Task WhenPayloadDoesNotIncludePaymentMethodIdInValidSubmitRequest_ThenReturnSuccessWithExpectedPayload()
    {
        // Arrange
        var mockedPaymentRequest = _fixture.Create<StripeExePaymentRequestDto>();
        var organisationId = mockedPaymentRequest.OrganisationId;
        var correlationId = Guid.NewGuid();
        var providerAccountId = GenerateMockStripeAccountId();

        var submitPaymentRequest = new StripeExeSubmitPaymentRequestDto()
        {
            PaymentRequest = mockedPaymentRequest,
            PaymentMethodsMadeAvailable = ["card"]
        };

        _pact.UponReceiving("A POST request to the submit stripe execution endpoint for a valid request without PaymentMethodId")
            .WithRequest(HttpMethod.Post, Constants.StripeSubmitEndpoint)
                .WithHeader(ExecutionConstants.XeroTenantId, organisationId.ToString())
                .WithHeader(Constants.ProviderAccountId, providerAccountId)
                .WithHeader(ExecutionConstants.XeroCorrelationId, correlationId.ToString())
                .WithHeader(ExecutionConstants.Authorization, Constants.StripeSubmit)
                .WithJsonBody(submitPaymentRequest)
            .WillRespond()
                .WithJsonBody(new
                {
                    clientSecret = Match.Regex("pi_12345_secret_abcd1234", @"^[a-zA-Z0-9_-]+"),
                    paymentIntentId = Match.Regex("pi_12345", @"^[a-zA-Z0-9_-]+"),
                    providerServiceId = Match.Regex(organisationId.ToString(), @"^[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}$")
                })
                .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            var apiClient = StripeExecutionConsumerPactTest.CreateStripeExecutionClient(
                ctx, Constants.StripeSubmit, organisationId, correlationId, providerAccountId);

            var response = await apiClient.SubmitStripeExecutionAsync(submitPaymentRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    private string GenerateMockStripeAccountId()
    {
        var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomPart = string.Join("", _fixture.CreateMany<char>(10).Select(c => chars[Math.Abs(c) % chars.Length]));
        return $"acct_{randomPart}";
    }
}
