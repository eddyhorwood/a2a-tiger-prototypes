using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;

namespace PaymentExecutionService.ComponentTests;

[Collection("NoParallelizationCollection")]
public class RequestCancelComponentTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>, IAsyncLifetime
{
    private const string RequestCancelScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.RequestCancel;
    private const string RequestCancelEndpointTemplate = "v1/payments/request-cancel/{paymentRequestId}";

    private readonly string _defaultValidUri =
        RequestCancelEndpointTemplate.Replace("{paymentRequestId}", Guid.NewGuid().ToString());

    private readonly HttpClient _client = fixture.CreateAuthenticatedClient(RequestCancelScope)
        .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
        .WithXeroCorrelationId()
        .WithXeroTenantId();

    private readonly RequestCancelPayload _defaultValidPayload = new()
    {
        CancellationReason = "User requested cancellation"
    };
    
    [Theory, AutoData]
    public async Task GivenCancellablePaymentTransaction_WhenRequestCancelEndpoint_ThenReturnsOkAndMessageSentToQueue(PaymentTransactionDto mockedSubmittedDto)
    {
        // Arrange
        mockedSubmittedDto.Status = nameof(TransactionStatus.Submitted);
        mockedSubmittedDto.ProviderType = nameof(ProviderType.Stripe);
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockedSubmittedDto);
        var requestContent = CreateStringContent(_defaultValidPayload);
        var uri = RequestCancelEndpointTemplate.Replace("{paymentRequestId}", mockedSubmittedDto.PaymentRequestId.ToString());

        // Act
        var response = await _client.PostAsync(uri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        
        // Verify message was sent to the cancel execution queue
        var queueMessages = await fixture.SqsUtility.ReceiveCancelMessagesAsync(waitTimeSeconds: 2);
        queueMessages.Messages.Should().NotBeEmpty("a message should be sent to the cancel execution queue");
        queueMessages.Messages.Should().HaveCount(1);
        
        var messageBody = queueMessages.Messages[0].Body;
        messageBody.Should().Contain(mockedSubmittedDto.PaymentRequestId.ToString());
        messageBody.Should().Contain(_defaultValidPayload.CancellationReason);
    }

    [Theory, AutoData]
    public async Task GivenTransactionWithTerminalStatus_WhenRequestCancelEndpoint_ThenReturnsBadRequest(PaymentTransactionDto mockedSubmittedDto)
    {
        // Arrange
        mockedSubmittedDto.Status = nameof(TransactionStatus.Succeeded);
        mockedSubmittedDto.ProviderType = nameof(ProviderType.Stripe);
        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockedSubmittedDto);
        var requestContent = CreateStringContent(_defaultValidPayload);
        var uri = RequestCancelEndpointTemplate.Replace("{paymentRequestId}", mockedSubmittedDto.PaymentRequestId.ToString());

        // Act
        var response = await _client.PostAsync(uri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory, AutoData]
    public async Task GivenTransactionWithoutProviderPaymentTransactionId_WhenRequestCancelEndpoint_ThenReturnsBadRequest(PaymentTransactionDto mockedSubmittedDto)
    {
        // Arrange
        mockedSubmittedDto.PaymentProviderPaymentTransactionId = null;
        mockedSubmittedDto.Status = nameof(TransactionStatus.Submitted);
        mockedSubmittedDto.ProviderType = nameof(ProviderType.Stripe);

        await fixture.GetTestingPaymentTransactionRepositoryInterface()
            .InsertMockSubmittedPaymentTransaction(mockedSubmittedDto);
        var requestContent = CreateStringContent(_defaultValidPayload);
        var uri = RequestCancelEndpointTemplate.Replace("{paymentRequestId}", mockedSubmittedDto.PaymentRequestId.ToString());

        // Act
        var response = await _client.PostAsync(uri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenPaymentTransactionDoesNotExist_WhenRequestCancelEndpoint_ThenReturnsNotFound()
    {
        // Arrange
        var requestContent = CreateStringContent(_defaultValidPayload);
        var uri = RequestCancelEndpointTemplate.Replace("{paymentRequestId}", Guid.NewGuid().ToString());

        // Act
        var response = await _client.PostAsync(uri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenInvalidPaymentRequestIdInQuery_WhenRequestCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var uri = RequestCancelEndpointTemplate.Replace("{paymentRequestId}", "not-a-valid-payment-request-Id!");

        // Act
        var response = await _client.PostAsync(uri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenEmptyRequestBody_WhenRequestCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(_defaultValidUri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenEmptyCancellationReason_WhenRequestCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var invalidPayload = new RequestCancelPayload
        {
            CancellationReason = string.Empty
        };
        var requestContent = CreateStringContent(invalidPayload);

        // Act
        var response = await _client.PostAsync(_defaultValidUri, requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenInvalidAccessToken_WhenRequestCancelEndpoint_ThenReturnsUnauthorized()
    {
        // Arrange
        var mockedClient = fixture.CreateAuthenticatedClientWithTenantId(RequestCancelScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithXeroTenantId();
        mockedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "Invalid token");

        // Act
        var response = await mockedClient.PostAsync(_defaultValidUri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenInvalidScope_WhenRequestCancelEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var inappropriateScope = PaymentExecutionService.Constants.ServiceAuthorizationScopes.Submit;
        var clientWithInvalidScope = fixture.CreateAuthenticatedClientWithTenantId(inappropriateScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithInvalidScope.PostAsync(_defaultValidUri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenNonWhitelistedClientId_WhenRequestCancelEndpoint_ThenReturnsForbidden()
    {
        // Arrange
        var clientWithNonWhitelistedClientId = fixture
            .CreateAuthenticatedClientWithClientId(ComponentTestsFixture.NonWhitelistedClientId, RequestCancelScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId()
            .WithXeroTenantId();

        // Act
        var response = await clientWithNonWhitelistedClientId.PostAsync(_defaultValidUri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenMissingTenantIdHeader_WhenRequestCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingTenantIdHeader = fixture.CreateAuthenticatedClientWithTenantId(RequestCancelScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId();
        clientMissingTenantIdHeader.DefaultRequestHeaders.Remove("Xero-Tenant-Id");

        // Act
        var response = await clientMissingTenantIdHeader.PostAsync(_defaultValidUri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenMissingCorrelationIdHeader_WhenRequestCancelEndpoint_ThenReturnsBadRequest()
    {
        // Arrange
        var clientMissingCorrelationIdHeader = fixture.CreateAuthenticatedClientWithTenantId(RequestCancelScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroTenantId();

        // Act
        var response = await clientMissingCorrelationIdHeader.PostAsync(_defaultValidUri, CreateStringContent(_defaultValidPayload));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static StringContent CreateStringContent(RequestCancelPayload payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload, ComponentTestsFixture.DefaultJsonOpts), System.Text.Encoding.UTF8,
            "application/json");
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await fixture.WipePaymentTransactionDb();
        await fixture.SqsUtility.PurgeCancelQueueAsync();
    }
}
