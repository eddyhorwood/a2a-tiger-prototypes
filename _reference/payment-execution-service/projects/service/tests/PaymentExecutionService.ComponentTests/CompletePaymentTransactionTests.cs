using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;

namespace PaymentExecutionService.ComponentTests;

public class CompletePaymentTransactionTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>
{
    private const string CompleteScope =
        PaymentExecutionService.Constants.ServiceAuthorizationScopes.Complete;
    private readonly HttpClient _client = fixture
        .CreateAuthenticatedClientWithTenantId(CompleteScope)
        .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
        .WithXeroCorrelationId();
    private readonly HttpClient _unauthenticatedClient = fixture.CreateUnauthenticatedClient();
    private readonly HttpClient _clientNoCorrelationId = fixture
        .CreateAuthenticatedClientWithTenantId(PaymentExecutionService.Constants.ServiceAuthorizationScopes
            .Complete);
    private readonly JsonSerializerOptions _defaultOpts =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

    [Fact]
    public async Task GivenValidRequestBodyWithRequiredHeaders_WhenCompletePaymentTransaction_ThenReturnsAccepted202()
    {
        var validRequestBody = CreateValidRequestJsonString();
        var content = new StringContent(validRequestBody, System.Text.Encoding.UTF8, "application/json");

        var validEndpointUrl = CreateUrlWithValidRouteParameters();
        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);

        //Cleanup
        await fixture.SqsUtility.PurgeQueueAsync();
    }

    [Theory]
    [InlineAutoData(0.1)]
    [InlineAutoData(4.49)]
    [InlineAutoData(0.99)]
    [InlineAutoData(2.5)]
    public async Task
        GivenValidRequestBodyWithDecimalFee_WhenCompletePaymentTransaction_ThenPopulatesQueueWithoutRoundingFee(
            decimal fee, CompletePaymentTransactionRequest request)
    {
        //Arrange
        request.Fee = fee;
        var validRequestString = JsonSerializer.Serialize(request, _defaultOpts);
        var content = new StringContent(validRequestString, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        //Act
        var result = await _client.PostAsync(validEndpointUrl, content);

        //Assert on response and check that fee has not been rounded
        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var queueResponse = await fixture.SqsUtility.ReceiveMessagesAsync();
        queueResponse.Messages.Count.Should().Be(1);
        var messageBody = JsonSerializer.Deserialize<ExecutionQueueMessage>(queueResponse.Messages[0].Body, _defaultOpts);

        messageBody?.Fee.Should().Be(fee);

        //Cleanup
        await fixture.SqsUtility.PurgeQueueAsync();
    }

    [Fact]
    public async Task GivenValidRequestBodyWithoutTenantIdHeader_WhenCompletePaymentTransaction_ThenReturnsBadRequest()
    {
        var validRequestBody = CreateValidRequestJsonString();
        var content = new StringContent(validRequestBody, System.Text.Encoding.UTF8, "application/json");

        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        _client.DefaultRequestHeaders.Remove("Xero-Tenant-Id");

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenValidRequestBodyWithoutCorrelationIdHeader_WhenCompletePaymentTransaction_ThenReturnsBadRequest()
    {
        var validRequestBody = CreateValidRequestJsonString();
        var content = new StringContent(validRequestBody, System.Text.Encoding.UTF8, "application/json");

        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        _client.DefaultRequestHeaders.Remove("Xero-Correlation-Id");

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Stripe", "non-terminal-status")]
    [InlineData("fake-provider", "Succeeded")]
    public async Task GivenInvalidEnumValue_WhenCompletePaymentTransaction_ThenReturnsBadRequest400(string providerType, string status)
    {
        var invalidRequestBody = CreateInvalidRequestJsonString(providerType, status);
        var content = new StringContent(invalidRequestBody, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #region Validation on Succeed Status Required Attributes
    [Fact]
    public async Task GivenSucceededStatusAndNoProviderTransactionReference_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("ch_1234", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenSucceededStatusAndNoFee_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("5", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenSucceededStatusAndNoFeeCurrency_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("aud", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenSucceededStatusAndNoPaymentProviderPaymentTransactionId_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("pi_1234", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenSucceededStatusAndNoPaymentProviderLastUpdatedAt_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("2025-04-01T10:21:15Z", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion

    #region Validation on Failed Status Required Attributes
    [Fact]
    public async Task GivenFailedStatusAndNoProviderTransactionReference_WhenCompletedPaymentTransaction_ThenReturnsAccepted()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("Succeeded", "Failed")
            .Replace("MyTransActionRef", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
    [Fact]
    public async Task GivenFailedStatusAndNoFailureDetails_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("Succeeded", "Failed")
            .Replace("tes_failure_error", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Validation on Cancelled Status Required Attributes
    [Fact]
    public async Task GivenCancelledStatusAndEmptyCancellationReason_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidCancellationReason = CreateValidRequestJsonString()
            .Replace("Succeeded", "Cancelled")
            .Replace("CancellationReason", "");
        var content = new StringContent(requestBodyWithInvalidCancellationReason, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenCancelledStatusAndNoPaymentProviderPaymentTransactionId_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidCancellationReason = CreateValidRequestJsonString()
            .Replace("Succeeded", "Cancelled")
            .Replace("pi_1234", null);
        var content = new StringContent(requestBodyWithInvalidCancellationReason, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GivenCancelledStatusAndNoPaymentProviderLastUpdatedAt_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var requestBodyWithInvalidTransactionRef = CreateValidRequestJsonString()
            .Replace("Succeeded", "Cancelled")
            .Replace("2025-04-01T10:21:15Z", null);
        var content = new StringContent(requestBodyWithInvalidTransactionRef, System.Text.Encoding.UTF8, "application/json");
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await _client.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion

    [Theory]
    [InlineData("bf602d4a-f3bb-4c23-b83b-620c5c992caf", "invalid-guid")]
    [InlineData("invalid-guid", "bf602d4a-f3bb-4c23-b83b-620c5c992caf")]
    public async Task GivenInvalidRoutParameters_WhenCompletePaymentTransaction_ThenReturnsBadRequest400(
        string paymentRequestId, string providerServiceId)
    {
        var content = new StringContent(CreateValidRequestJsonString(), System.Text.Encoding.UTF8, "application/json");
        var urlWithRouteParameters = CreateEndpointUrl(paymentRequestId, providerServiceId);

        var result = await _client.PostAsync(urlWithRouteParameters, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenNoCorrelationId_WhenCompletePaymentTransaction_ThenReturnsBadRequest400()
    {
        var validRequestBody = CreateValidRequestJsonString();
        var content = new StringContent(validRequestBody, System.Text.Encoding.UTF8, "application/json");
        var urlWithValidParameters = CreateUrlWithValidRouteParameters();

        var result = await _clientNoCorrelationId.PostAsync(urlWithValidParameters, content);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenInvalidAccessToken_WhenCompletePaymentTransaction_ThenReturnsUnauthorized401()
    {
        var content = new StringContent(CreateValidRequestJsonString(), System.Text.Encoding.UTF8, "application/json");
        var urlWithValidParameters = CreateUrlWithValidRouteParameters();

        var result = await _unauthenticatedClient.PostAsync(urlWithValidParameters, content);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenValidTokenWithNonWhitelistedClientId_WhenCompletePaymentTransaction_ThenReturnsForbidden()
    {
        var validRequestBody = CreateValidRequestJsonString();
        var content = new StringContent(validRequestBody, System.Text.Encoding.UTF8, "application/json");
        var clientWithNonWhitelistedClientId =
            fixture.CreateAuthenticatedClientWithClientId("local_caller_non-whitelisted", CompleteScope)
                .WithXeroCorrelationId()
                .WithXeroTenantId();
        var validEndpointUrl = CreateUrlWithValidRouteParameters();

        var result = await clientWithNonWhitelistedClientId.PostAsync(validEndpointUrl, content);

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string CreateValidRequestJsonString()
    {
        return """
               
                       {
                           "Fee":"5",
                           "FeeCurrency":"aud",
                           "ProviderType": "Stripe",
                           "PaymentProviderPaymentTransactionId": "pi_1234",
                           "PaymentProviderPaymentReferenceId": "ch_1234",
                           "Status": "Succeeded",
                           "FailureDetails": "tes_failure_error",
                           "EventCreatedDateTime":"2025-04-01T10:21:15Z",
                           "PaymentProviderLastUpdatedAt":"2025-04-01T10:21:15Z",
                           "CancellationReason": "User requested cancellation"
                         }
               """;
    }

    private static string CreateInvalidRequestJsonString(string providerType, string status)
    {
        return $@"
        {{
            ""Fee"":""5"",
            ""FeeCurrency"":""aud"",
            ""ProviderType"": ""{providerType}"",
            ""PaymentProviderPaymentTransactionId"": ""MyTransActionRef"",
            ""Status"": ""{status}"",
            ""EventCreatedDateTime"":""2025-04-01T10:21:15Z"",
            ""PaymentProviderLastUpdatedAt"":""2025-04-01T10:21:15Z""
          }}";
    }

    private static string CreateEndpointUrl(string paymentRequestId, string providerServiceId)
    {
        return $"{Constants.Endpoints.PaymentTransactionsRoot}/payment-requests/{paymentRequestId}/provider-executions/{providerServiceId}/complete";
    }

    private static string CreateUrlWithValidRouteParameters()
    {
        var paymentRequestId = Guid.NewGuid().ToString();
        var providerServiceId = Guid.NewGuid().ToString();
        return CreateEndpointUrl(paymentRequestId, providerServiceId);
    }
}
