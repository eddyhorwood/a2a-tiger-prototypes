using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;
using HeaderConstants = Xero.Accelerators.Api.Core.Constants.HttpHeaders;

namespace PaymentExecutionService.ComponentTests;

[Collection("NoParallelizationCollection")]
public class GetProviderStateTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>
{
    private const string ReadProviderStateScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.ReadProviderState;
    private const string GetProviderStateEndpointTemplate = "v1/payments/provider-state?paymentRequestId={paymentRequestId}";
    private const string StripeExeSuccessResponseTenantId = "55159241-3379-4884-8b38-bd730fe8ea9b";
    private const string StripeExeNonTransientErrorResponseTenantId = "c7dd73bf-ed5d-4970-b0e4-801a0ec44def";
    private const string StripeExeTransientErrorResponseTenantId = "68fbe448-691e-45d2-a328-df7ac3db6c90";

    private readonly string _defaultValidUri =
        GetProviderStateEndpointTemplate.Replace("{paymentRequestId}", Guid.NewGuid().ToString());

    private readonly HttpClient _defaultValidClient =
        fixture.CreateAuthenticatedClient(ReadProviderStateScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

    [Fact]
    public async Task GivenValidRequest_WhenGetProviderStateEndpoint_ThenReturnsOkWithExpectedResponseBody()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var mockedPaymentIntentId = "pi_mockedpi123"; //Wiremock hardcoded payment intent
        SetUpClientTenantIdWithValue(_defaultValidClient, StripeExeSuccessResponseTenantId);

        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = testPaymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = mockedPaymentIntentId,
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        var uri = GetProviderStateEndpointTemplate.Replace("{paymentRequestId}", testPaymentRequestId.ToString());
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);

        // Act
        var response = await _defaultValidClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadFromJsonAsync<ProviderState>();
        // Based on hardcoded wiremock response
        var expectedProviderState = new ProviderState()
        {
            PaymentProviderPaymentTransactionId = mockedPaymentIntentId,
            PaymentProviderStatus = PaymentProviderStatus.RequiresAction,
            ProviderType = ProviderType.Stripe,
            LastPaymentErrorCode = null,
            PendingStatusDetails = new PendingStatusDetails()
            {
                PaymentMethodType = PaymentMethodType.Klarna,
                RequiresActionType = RequiresActionType.RedirectToUrl,
                HasActionValue = true,
                RedirectToUrl = new RedirectToUrl()
                {
                    RedirectUrl = "https://hooks.stripe.com/3d_secure_redirect/mock_redirect"
                }
            }
        };
        responseBody.Should().NotBeNull().And.BeEquivalentTo(expectedProviderState);
    }

    [Fact]
    public async Task GivenStripeExecutionReturnsNonTransientError_WhenGetProviderStateEndpoint_ThenReturnsExpectedErrorResponse()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var mockedPaymentIntentId = "pi_mockedpi123"; //Wiremock hardcoded payment intent
        SetUpClientTenantIdWithValue(_defaultValidClient, StripeExeNonTransientErrorResponseTenantId);

        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = testPaymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = mockedPaymentIntentId,
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        var uri = GetProviderStateEndpointTemplate.Replace("{paymentRequestId}", testPaymentRequestId.ToString());
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);

        // Act
        var response = await _defaultValidClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.FailedDependency);
        var responseBody = await response.Content.ReadFromJsonAsync<ProblemDetailsExtended>();
        responseBody.Should().NotBeNull();
        responseBody.Detail.Should().Contain("DB record does not exist for the provided payment request ID");
    }

    [Fact]
    public async Task GivenStripeExecutionReturnsTransientError_WhenGetProviderStateEndpoint_ThenReturnsExpectedResponse()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var mockedPaymentIntentId = "pi_mockedpi123"; //Wiremock hardcoded payment intent
        SetUpClientTenantIdWithValue(_defaultValidClient, StripeExeTransientErrorResponseTenantId);

        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = testPaymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = mockedPaymentIntentId,
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        var uri = GetProviderStateEndpointTemplate.Replace("{paymentRequestId}", testPaymentRequestId.ToString());
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);

        // Act
        var response = await _defaultValidClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var responseBody = await response.Content.ReadFromJsonAsync<ProblemDetailsExtended>();
        responseBody.Should().NotBeNull();
        responseBody.Detail.Should().Contain("Service is temporarily unavailable, please try again later");
    }

    [Fact]
    public async Task GivenNotExistentPaymentRequestId_WhenGetProviderStateEndpoint_ThenReturnsNotFound()
    {
        // Act
        var response = await _defaultValidClient.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenMissingTenantIdHeader_WhenGetProviderStateEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingTenantIdHeader = fixture.CreateAuthenticatedClient(ReadProviderStateScope)
            .WithXeroCorrelationId();

        // Act
        var response = await clientMissingTenantIdHeader.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenMissingCorrelationIdHeader_WhenGetProviderStateEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingCorrelationIdHeader = fixture.CreateAuthenticatedClient(ReadProviderStateScope)
            .WithXeroTenantId();

        // Act
        var response = await clientMissingCorrelationIdHeader.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenRequestMissingQueryParameter_WhenGetProviderStateEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var uriWithoutQueryParam = "v1/payments/provider-state";

        // Act
        var response = await _defaultValidClient.GetAsync(uriWithoutQueryParam);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenRequestWithInvalidQueryParameter_WhenGetProviderStateEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var uriWithInvalidQueryParam = GetProviderStateEndpointTemplate.Replace("{paymentRequestId}", "invalid-guid");

        // Act
        var response = await _defaultValidClient.GetAsync(uriWithInvalidQueryParam);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenInvalidIdentityToken_WhenGetProviderStateEndpoint_ThenReturnsUnauthorized()
    {
        // Arrange
        var mockedClient = fixture.CreateAuthenticatedClient(ReadProviderStateScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();
        mockedClient.DefaultRequestHeaders.Remove("Bearer");
        mockedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "Invalid token");

        // Act
        var response = await mockedClient.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenInvalidScope_WhenGetProviderStateEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var inappropriateScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.Submit;
        var clientWithInvalidScope = fixture.CreateAuthenticatedClient(inappropriateScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithInvalidScope.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenNonWhitelistedClientId_WhenGetProviderStateEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var clientWithNonWhitelistedClientId = fixture
            .CreateAuthenticatedClientWithClientId(ComponentTestsFixture.NonWhitelistedClientId, ReadProviderStateScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithNonWhitelistedClientId.GetAsync(_defaultValidUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void SetUpClientTenantIdWithValue(HttpClient client, string identifierValue)
    {
        client.DefaultRequestHeaders.Remove(HeaderConstants.XeroTenantId);
        client.DefaultRequestHeaders.Add(HeaderConstants.XeroTenantId, identifierValue);
    }
}
