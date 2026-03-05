using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;
using HeaderConstants = Xero.Accelerators.Api.Core.Constants.HttpHeaders;

namespace PaymentExecutionService.ComponentTests;

[Collection("NoParallelizationCollection")]
public class CancelTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>, IAsyncLifetime
{
    private const string CancelScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.Cancel;
    private const string CancelEndpointTemplate = "v1/payments/cancel/{paymentRequestId}";
    private readonly WiremockApi _stripeExecutionWiremockApi = new("http://localhost:12112");

    private readonly string _defaultValidUri =
        CancelEndpointTemplate.Replace("{paymentRequestId}", Guid.NewGuid().ToString());

    private readonly StringContent _defaultValidRequestPayload =
        new StringContent(
            JsonSerializer.Serialize(new CancelPayload() { CancellationReason = "Customer requested cancellation" }),
            System.Text.Encoding.UTF8,
            "application/json");

    private readonly JsonSerializerOptions _testSuiteJsonSerializerOptions =
        new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _defaultValidClient =
        fixture.CreateAuthenticatedClient(CancelScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

    [Fact]
    public async Task GivenValidRequest_WhenCancelEndpoint_ThenReturnsNoContent()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = "pi_mockedpi123",
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);
        var uri = CancelEndpointTemplate.Replace("{paymentRequestId}", paymentRequestId.ToString());
        SetUpClientTenantIdWithValue(_defaultValidClient,
            Constants.StripeExeWireMockGuids.StripeExeSuccessResponseTenantId);

        // Act
        var response = await _defaultValidClient.PostAsync(uri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GivenTransientErrorResponseFromStripeExe_WhenCancelEndpoint_ThenReturnsExpectedResponse()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = "pi_mockedpi123",
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);
        var uri = CancelEndpointTemplate.Replace("{paymentRequestId}", paymentRequestId.ToString());
        SetUpClientTenantIdWithValue(_defaultValidClient,
            Constants.StripeExeWireMockGuids.StripeExeTransientErrorResponseTenantId);

        // Act
        var response = await _defaultValidClient.PostAsync(uri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsExtended>();
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.ServiceUnavailable,
            Title = nameof(HttpStatusCode.ServiceUnavailable),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",
            ErrorCode = ErrorConstants.ErrorCode.ExecutionCancellationError,
            Detail = "Service is temporarily unavailable, please try again later.",
        };
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails,
            opts => opts.Excluding(p => p.Extensions));
    }

    [Fact]
    public async Task GivenTransientErrorResponseFromStripeExe_WhenCancelEndpoint_ThenCallIsRetriedExpectedTimes()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = "pi_mockedpi123",
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);
        var uri = CancelEndpointTemplate.Replace("{paymentRequestId}", paymentRequestId.ToString());
        SetUpClientTenantIdWithValue(_defaultValidClient,
            Constants.StripeExeWireMockGuids.StripeExeTransientErrorResponseTenantId);

        // Act
        var response = await _defaultValidClient.PostAsync(uri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var wiremockResult = await _stripeExecutionWiremockApi.GetRequests();
        var requests = wiremockResult.Where(r =>
                r.Request != null && r.Request.Headers.TryGetValue("Xero-Tenant-Id", out var xeroTenantId)
                                  && xeroTenantId == Constants.StripeExeWireMockGuids.StripeExeTransientErrorResponseTenantId
                && r.Request.Url == $"/v1/payments/cancel/{paymentRequestId}"
            )
            .ToList();

        var expectedPollyRetries = 3;
        requests.Count.Should().Be(expectedPollyRetries + 1); // The +1 includes the intiial request
    }

    [Fact]
    public async Task GivenNonTransientErrorResponseFromStripeExe_WhenCancelEndpoint_ThenReturnsExpectedResponse()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var wiremockDefinedDetail = "Payment intent is not cancellable";
        var wiremockDefinedProviderErrorCode = "invalid_request_error";
        var mockPaymentTransactionDto = new PaymentTransactionDto()
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = "pi_mockedpi123",
            ProviderServiceId = Guid.NewGuid(),
            Status = "Submitted",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockPaymentTransactionDto);
        var uri = CancelEndpointTemplate.Replace("{paymentRequestId}", paymentRequestId.ToString());
        SetUpClientTenantIdWithValue(_defaultValidClient,
            Constants.StripeExeWireMockGuids.StripeExeNonTransientErrorResponseTenantId);

        // Act
        var response = await _defaultValidClient.PostAsync(uri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsExtended>();
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = nameof(HttpStatusCode.BadRequest),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Detail = wiremockDefinedDetail,
            ErrorCode = ErrorConstants.ErrorCode.ExecutionCancellationError,
            ProviderErrorCode = wiremockDefinedProviderErrorCode
        };
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails,
            opts => opts.Excluding(p => p.Extensions));
    }

    [Fact]
    public async Task GivenNonExistentPaymentRequest_WhenCancelEndpoint_ThenReturnsNotFound()
    {
        // Arrange
        var randomGuid = Guid.NewGuid();
        var uri = CancelEndpointTemplate.Replace("{paymentRequestId}", randomGuid.ToString());

        // Act
        var response = await _defaultValidClient.PostAsync(uri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenInvalidRequestBody_WhenCancelEndpoint_ThenReturnsExpectedBadRequest()
    {
        // Arrange
        var invalidRequestPayload = new StringContent(
            JsonSerializer.Serialize(new { InvalidField = "Invalid data" }),
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _defaultValidClient.PostAsync(_defaultValidUri, invalidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(
            await response.Content.ReadAsStringAsync(),
            _testSuiteJsonSerializerOptions
        );
        responseContent!.Extensions["errors"]!.ToString().Should()
            .Contain("The cancelPayload field is required");
    }

    [Fact]
    public async Task GivenMissingTenantIdHeader_WhenCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingTenantIdHeader = fixture.CreateAuthenticatedClient(CancelScope)
            .WithXeroCorrelationId();

        // Act
        var response = await clientMissingTenantIdHeader.PostAsync(_defaultValidUri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(
            await response.Content.ReadAsStringAsync(),
            _testSuiteJsonSerializerOptions
        );
        responseContent!.Detail.Should()
            .Contain("The Xero-Tenant-Id header is required.");
    }

    [Fact]
    public async Task GivenMissingCorrelationIdHeader_WhenCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingCorrelationIdHeader = fixture.CreateAuthenticatedClient(CancelScope)
            .WithXeroTenantId();

        // Act
        var response = await clientMissingCorrelationIdHeader.PostAsync(_defaultValidUri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(
            await response.Content.ReadAsStringAsync(),
            _testSuiteJsonSerializerOptions
        );
        responseContent!.Detail.Should()
            .Contain("The Xero-Correlation-Id header is required.");
    }

    [Fact]
    public async Task GivenRequestWithInvalidRouteParameter_WhenCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var uriWithInvalidRouteParam = CancelEndpointTemplate.Replace("{paymentRequestId}", "invalid-guid");

        // Act
        var response = await _defaultValidClient.PostAsync(uriWithInvalidRouteParam, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(
            await response.Content.ReadAsStringAsync(),
            _testSuiteJsonSerializerOptions
        );
        responseContent!.Extensions["errors"]!.ToString().Should()
            .Contain("\"paymentRequestId\":[\"The value 'invalid-guid' is not valid.\"]");
    }

    [Fact]
    public async Task GivenInvalidIdentityToken_WhenCancelEndpoint_ThenReturnsUnauthorized()
    {
        // Arrange
        var mockedClient = fixture.CreateAuthenticatedClient(CancelScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();
        mockedClient.DefaultRequestHeaders.Remove("Bearer");
        mockedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "Invalid token");

        // Act
        var response = await mockedClient.PostAsync(_defaultValidUri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenInvalidScope_WhenCancelEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var inappropriateScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.Submit;
        var clientWithInvalidScope = fixture.CreateAuthenticatedClient(inappropriateScope)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithInvalidScope.PostAsync(_defaultValidUri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenNonWhitelistedClientId_WhenCancelEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var clientWithNonWhitelistedClientId = fixture
            .CreateAuthenticatedClientWithClientId(ComponentTestsFixture.NonWhitelistedClientId, CancelScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithNonWhitelistedClientId.PostAsync(_defaultValidUri, _defaultValidRequestPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void SetUpClientTenantIdWithValue(HttpClient client, string identifierValue)
    {
        client.DefaultRequestHeaders.Remove(HeaderConstants.XeroTenantId);
        client.DefaultRequestHeaders.Add(HeaderConstants.XeroTenantId, identifierValue);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await fixture.GetTestingPaymentTransactionRepositoryInterface().WipeDb();
    }
}
