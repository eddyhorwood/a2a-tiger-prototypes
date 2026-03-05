using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.TestUtilities;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;

namespace PaymentExecutionService.ComponentTests;

[Collection("NoParallelizationCollection")]
public class SubmitStripePaymentTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>, IAsyncLifetime
{
    private const string SubmitScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.Submit;
    private readonly HttpClient _client = fixture.CreateAuthenticatedClientWithTenantId(SubmitScope)
        .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
        .WithXeroCorrelationId()
        .WithProviderAccountId();

    private readonly WiremockApi _paymentRequestWiremockApi = new("http://localhost:12111");
    private readonly WiremockApi _stripeExecutionWiremockApi = new("http://localhost:12112");

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    [Theory]
    [InlineData("some-method-id", null)]
    [InlineData(null, "Card")]
    public async Task GivenValidAuthorizedRequest_WhenSubmitPaymentTransactionEndpoint_ThenReturnsOkAndMessageSentToQueue(
        string? paymentMethodId, string? paymentMethod)
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent(paymentMethodId, paymentMethod,
            paymentRequestId: testPaymentRequestId.ToString());

        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);
        
        // Assert 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify message was sent to the cancel execution queue
        var queueMessages = await fixture.SqsUtility.ReceiveCancelMessagesAsync(waitTimeSeconds: 2);
        queueMessages.Messages.Should().NotBeEmpty("a message should be sent to the cancel execution queue");
        queueMessages.Messages.Should().HaveCount(1);
        
        // Verify the message body contains the expected paymentRequestId
        var message = queueMessages.Messages.Single();
        message.Body.Should().Contain(testPaymentRequestId.ToString(),
            "the cancel execution message should reference the submitted payment");
    }

    [Theory]
    [InlineData("some-method-id", null)]
    [InlineData(null, "Card")]
    public async Task GivenValidAuthorizedRequest_WhenSubmitPaymentTransactionEndpoint_ThenReturnsCreatedResponse(
        string? paymentMethodId, string? paymentMethod)
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent(paymentMethodId, paymentMethod,
            paymentRequestId: testPaymentRequestId.ToString());

        //Arbitrary values defined in the wiremock server
        var expectedClientSecret = "pssst-secret";
        var expectedPaymentIntentId = "pi_1234567890";
        var expectedProviderServiceId = "ead3cba4-b845-493e-af36-8d7a37204766";

        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        var responseContent = await response.Content.ReadAsStringAsync();

        //Assert response payload
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deserializedResponseContent =
            JsonSerializer.Deserialize<SubmitStripePaymentCommandResponse>(responseContent, _jsonSerializerOptions);
        Assert.Equal(expectedClientSecret, deserializedResponseContent?.ClientSecret);
        Assert.Equal(expectedPaymentIntentId, deserializedResponseContent?.PaymentIntentId);

        //Assert expected DB record
        var recordResult = await fixture.GetPaymentTransactionRepository()
            .GetPaymentTransactionsByPaymentRequestId(testPaymentRequestId);
        var record = recordResult.Value;
        Assert.NotNull(record);
        Assert.Equal(TransactionStatus.Submitted.ToString(), record.Status);
        Assert.Equal(ProviderType.Stripe.ToString(), record.ProviderType);
        Assert.Equal(expectedPaymentIntentId, record.PaymentProviderPaymentTransactionId);
        Assert.Equal(expectedProviderServiceId, record.ProviderServiceId.ToString());

        await fixture.WipePaymentTransactionDb();
    }

    [Fact]
    public async Task GivenPaymentRequestReturnsBadRequest_WhenSubmitPaymentTransactionEndpoint_ThenReturnsBadRequestWithExpectedErrorMessage()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id", null,
            paymentRequestId: testPaymentRequestId.ToString());

        //Get payment request to return bad request - that particular guid triggers 400
        _client.DefaultRequestHeaders.Remove("Xero-Tenant-Id");
        _client.DefaultRequestHeaders.Add("Xero-Tenant-Id", "11111111-2222-3333-4444-555555555555");

        //act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserializedResponseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(responseContent, _jsonSerializerOptions);
        Assert.Equal(ErrorConstants.ErrorMessage.SubmitPaymentRequestError, deserializedResponseContent?.Detail);

        await fixture.WipePaymentTransactionDb();
    }
    [Fact]
    public async Task GivenValidAuthorizedRequest_WhenSubmitPaymentTransactionEndpointTwice_ThenReturnsOkAndIsIdempotent()
    {
        var paymentMethodId = "pm_1234567890";
        var paymentMethod = "Card"; // Arbitrary payment method for the test
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent(paymentMethodId, paymentMethod,
            paymentRequestId: testPaymentRequestId.ToString());

        //Arbitrary values defined in the wiremock server
        var expectedClientSecret = "pssst-secret";
        var expectedPaymentIntentId = "pi_1234567890";
        var expectedProviderServiceId = "ead3cba4-b845-493e-af36-8d7a37204766";

        await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        var initialInsertRecordResult = await fixture.GetPaymentTransactionRepository()
            .GetPaymentTransactionsByPaymentRequestId(testPaymentRequestId);
        var initialInsertRecord = initialInsertRecordResult.Value;

        Assert.NotNull(initialInsertRecord);

        var initialPaymentTransactionId = initialInsertRecord.PaymentTransactionId;

        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        var responseContent = await response.Content.ReadAsStringAsync();

        //Assert response payload
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deserializedResponseContent =
            JsonSerializer.Deserialize<SubmitStripePaymentCommandResponse>(responseContent, _jsonSerializerOptions);
        Assert.Equal(expectedClientSecret, deserializedResponseContent?.ClientSecret);
        Assert.Equal(expectedPaymentIntentId, deserializedResponseContent?.PaymentIntentId);

        //Assert expected DB record
        var recordResult = await fixture.GetPaymentTransactionRepository()
            .GetPaymentTransactionsByPaymentRequestId(testPaymentRequestId);
        var record = recordResult.Value;
        Assert.NotNull(record);
        Assert.Equal(initialPaymentTransactionId, record.PaymentTransactionId); //This check ensures that a new record was not created and the operation is idempotent
        Assert.Equal(TransactionStatus.Submitted.ToString(), record.Status);
        Assert.Equal(ProviderType.Stripe.ToString(), record.ProviderType);
        Assert.Equal(expectedPaymentIntentId, record.PaymentProviderPaymentTransactionId);
        Assert.Equal(expectedProviderServiceId, record.ProviderServiceId.ToString());

        await fixture.WipePaymentTransactionDb();
    }

    [Fact]
    public async Task GivenPaymentRequestReturnsTransientError_WhenSubmitPaymentTransactionEndpoint_ThenRetriesAndReturnsOk()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id", null,
            paymentRequestId: testPaymentRequestId.ToString());

        // This particular guid triggers a 500 error initially, and then subsequently returns 200
        const string CustomTenantId = "11111111-2222-3333-4444-555555555556";
        _client.DefaultRequestHeaders.Remove("Xero-Tenant-Id");
        _client.DefaultRequestHeaders.Add("Xero-Tenant-Id", CustomTenantId);

        //Arbitrary values defined in the wiremock server
        var expectedClientSecret = "pssst-secret";
        var expectedPaymentIntentId = "pi_1234567890";
        var expectedProviderServiceId = "ead3cba4-b845-493e-af36-8d7a37204766";

        //Act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        //Assert
        var result = await _paymentRequestWiremockApi.GetRequests();
        var requests = result.Where(r =>
                r.Request != null && r.Request.Headers.TryGetValue("Xero-Tenant-Id", out var xeroTenantId) && xeroTenantId == CustomTenantId
                && r.Request.Url == $"/v1/payment-requests/{testPaymentRequestId}/submit"
                && r.StubMapping?.ScenarioName == "TransientFailureScenario"
            )
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        requests.Count.Should().Be(2);

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserializedResponseContent = JsonSerializer.Deserialize<SubmitStripePaymentCommandResponse>(responseContent, _jsonSerializerOptions);
        Assert.Equal(expectedClientSecret, deserializedResponseContent?.ClientSecret);
        Assert.Equal(expectedPaymentIntentId, deserializedResponseContent?.PaymentIntentId);

        //Assert expected DB record
        var recordResult = await fixture.GetPaymentTransactionRepository()
            .GetPaymentTransactionsByPaymentRequestId(testPaymentRequestId);
        var record = recordResult.Value;
        Assert.NotNull(record);
        Assert.Equal(TransactionStatus.Submitted.ToString(), record.Status);
        Assert.Equal(ProviderType.Stripe.ToString(), record.ProviderType);
        Assert.Equal(expectedPaymentIntentId, record.PaymentProviderPaymentTransactionId);
        Assert.Equal(expectedProviderServiceId, record.ProviderServiceId.ToString());

        await fixture.WipePaymentTransactionDb();
    }

    [Fact]
    public async Task GivenStripeExecutionRequestReturnsTransientError_WhenSubmitPaymentTransactionEndpoint_ThenRetriesAndReturnsOk()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id", null,
            paymentRequestId: testPaymentRequestId.ToString());

        const string CustomAccountId = "transient-error-account-id";
        _client.DefaultRequestHeaders.Remove("Provider-Account-Id");
        _client.DefaultRequestHeaders.Add("Provider-Account-Id", CustomAccountId);

        //Arbitrary values defined in the wiremock server
        var expectedClientSecret = "pssst-secret";
        var expectedPaymentIntentId = "pi_1234567890";
        var expectedProviderServiceId = "ead3cba4-b845-493e-af36-8d7a37204766";

        //Act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        //Assert
        var result = await _stripeExecutionWiremockApi.GetRequests();
        var requests = result.Where(r =>
                r.Request != null && r.Request.Headers.TryGetValue("Provider-Account-Id", out var xeroTenantId) && xeroTenantId == CustomAccountId
                && r.Request.Url == "/v1/payments/submit"
                && r.StubMapping?.ScenarioName == "TransientFailureScenario"
            )
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        requests.Count.Should().Be(2);

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserializedResponseContent = JsonSerializer.Deserialize<SubmitStripePaymentCommandResponse>(responseContent, _jsonSerializerOptions);
        Assert.Equal(expectedClientSecret, deserializedResponseContent?.ClientSecret);
        Assert.Equal(expectedPaymentIntentId, deserializedResponseContent?.PaymentIntentId);

        //Assert expected DB record
        var recordResult = await fixture.GetPaymentTransactionRepository()
            .GetPaymentTransactionsByPaymentRequestId(testPaymentRequestId);
        var record = recordResult.Value;
        Assert.NotNull(record);
        Assert.Equal(TransactionStatus.Submitted.ToString(), record.Status);
        Assert.Equal(ProviderType.Stripe.ToString(), record.ProviderType);
        Assert.Equal(expectedPaymentIntentId, record.PaymentProviderPaymentTransactionId);
        Assert.Equal(expectedProviderServiceId, record.ProviderServiceId.ToString());

        await fixture.WipePaymentTransactionDb();
    }

    [Fact]
    public async Task
        GivenStripeExecutionReturns400Response_WhenSubmitPaymentTransactionEndpoint_ThenReturnsBadRequestWithExpectedMessageAndCode()
    {
        var testPaymentRequestId = Guid.NewGuid();
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id", null,
            paymentRequestId: testPaymentRequestId.ToString());
        var expectedDetail = "Invalid-account-id is not a valid stripe account"; //Defined in wiremock
        var expectedProviderErrorCode = "account-invalid"; //Defined in wiremock
        var expectedProblemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.PaymentRequired,
            Title = nameof(HttpStatusCode.PaymentRequired),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.2",
            Detail = expectedDetail,
            ErrorCode = ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed,
            ProviderErrorCode = expectedProviderErrorCode
        };
        //Trigger a bad request form stripe execution
        _client.DefaultRequestHeaders.Remove("Provider-Account-Id");
        _client.DefaultRequestHeaders.Add("Provider-Account-Id", "invalid-account-id");

        //Act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        //Assert
        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserializedResponseContent = JsonSerializer.Deserialize<ProblemDetailsExtended>(responseContent, _jsonSerializerOptions);
        Assert.Equivalent(expectedProblemDetails, deserializedResponseContent);

        await fixture.WipePaymentTransactionDb();
    }

    [Fact]
    public async Task GivenInvalidAccessToken_WhenSubmitStripeTransactionEndpoint_ThenReturnsUnauthorized()
    {
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "Invalid token");

        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(PaymentExecutionService.Constants.ServiceAuthorizationScopes.ReadOnly, HttpStatusCode.Forbidden)]
    [InlineData("fake-scope", HttpStatusCode.Unauthorized)]
    public async Task GivenInvalidScope_WhenSubmitStripeTransactionEndpoint_ThenReturnsExpected400SeriesResponse(string scope, HttpStatusCode expectedStatusCode)
    {
        HttpClient clientWithInvalidScope = fixture.CreateAuthenticatedClientWithTenantId(scope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithProviderAccountId();
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id");

        var response = await clientWithInvalidScope.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("some-payment-method-id", null, false)]
    [InlineData(null, "Card", false)]
    [InlineData("", null, true)]
    public async Task GivenInvalidRequestContent_WhenSubmitStripeTransactionEndpoint_ThenReturnsBadRequest(string? paymentMethodId, string? paymentMethod, bool isValidPaymentRequestId)
    {
        var requestContent = CreateSubmitStripeTransactionStringContent(paymentMethodId, paymentMethod, isValidPaymentRequestId: isValidPaymentRequestId);

        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    public async Task GivenInvalidProviderAccountIdHeader_WhenSubmitStripeTransactionEndpoint_ThenReturnsBadRequest(bool isHeaderPresent, string? invalidHeaderValue)
    {
        var clientWithInvalidProviderAccountId = CreateClientWithInvalidProviderAccountIdHeader(isHeaderPresent, invalidHeaderValue);
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id");

        var response = await clientWithInvalidProviderAccountId.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    [InlineData(true, "non-guid-tenant-id")]
    [InlineData(true, "00000000-0000-0000-0000-000000000000")]
    public async Task GivenInvalidXeroTenantIdHeader_WhenSubmitStripeTransactionEndpoint_ThenReturnsBadRequest(bool isHeaderPresent, string? invalidHeaderValue)
    {
        var clientWithInvalidProviderAccountId = CreateClientWithInvalidXeroTenantIdHeader(isHeaderPresent, invalidHeaderValue);
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id");

        var response = await clientWithInvalidProviderAccountId.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenUsingValidAccessTokenWithNonWhitelistedClientId_WhenStripeSubmit_ThenReturnsForbidden()
    {
        var requestContent = CreateSubmitStripeTransactionStringContent("some-method-id", null,
            Guid.NewGuid().ToString());
        var clientWithNonWhitelistedClientId =
            fixture.CreateAuthenticatedClientWithClientId("local_caller_non-whitelisted", SubmitScope)
                .WithProviderAccountId()
                .WithXeroCorrelationId()
                .WithXeroTenantId();

        var response = await clientWithNonWhitelistedClientId.PostAsync(Constants.Endpoints.SubmitStripePayment, requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClientWithInvalidProviderAccountIdHeader(bool isHeaderPresent, string? invalidHeaderValue)
    {
        HttpClient clientWithNoProviderIdHeader = fixture.CreateAuthenticatedClientWithTenantId(PaymentExecutionService.Constants
                .ServiceAuthorizationScopes
                .Submit)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId();

        if (!isHeaderPresent)
        {
            return clientWithNoProviderIdHeader;
        }

        var clientWithInvalidProviderIdValue = clientWithNoProviderIdHeader.WithProviderAccountId(invalidHeaderValue);
        return clientWithInvalidProviderIdValue;
    }

    private HttpClient CreateClientWithInvalidXeroTenantIdHeader(bool isHeaderPresent, string? invalidHeaderValue)
    {
        HttpClient clientWithNoXeroTenantIdHeader = fixture.CreateAuthenticatedClientWithTenantId(PaymentExecutionService.Constants
                .ServiceAuthorizationScopes
                .Submit)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithProviderAccountId();
        clientWithNoXeroTenantIdHeader.DefaultRequestHeaders.Remove("Xero-Tenant-Id");

        if (!isHeaderPresent)
        {
            return clientWithNoXeroTenantIdHeader;
        }

        clientWithNoXeroTenantIdHeader.DefaultRequestHeaders.Add("Xero-Tenant-Id", invalidHeaderValue);
        return clientWithNoXeroTenantIdHeader;
    }

    private static StringContent CreateSubmitStripeTransactionStringContent(string? paymentMethodId, string? paymentMethod = null,
        string paymentRequestId = "4a725f41-9b24-4947-902f-e7eea82efe4a", bool isValidPaymentRequestId = true)
    {
        var paymentMethodsMadeAvailableArray = paymentMethod == null ? null : new JsonArray(paymentMethod);
        var submitStripeRequest = new JsonObject()
        {
            ["paymentRequestId"] = isValidPaymentRequestId ? Guid.Parse(paymentRequestId) : "invalid-payment-request-id",
            ["paymentMethodId"] = paymentMethodId,
            ["paymentMethodsMadeAvailable"] = paymentMethodsMadeAvailableArray
        };

        return new StringContent(submitStripeRequest.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await fixture.WipePaymentTransactionDb();
        
        // Ensure each test runs with an empty queue
        await fixture.SqsUtility.PurgeCancelQueueAsync(); 

        await _paymentRequestWiremockApi.Reset();
        _paymentRequestWiremockApi.Dispose();

        await _stripeExecutionWiremockApi.Reset();
        _stripeExecutionWiremockApi.Dispose();
    }
}
