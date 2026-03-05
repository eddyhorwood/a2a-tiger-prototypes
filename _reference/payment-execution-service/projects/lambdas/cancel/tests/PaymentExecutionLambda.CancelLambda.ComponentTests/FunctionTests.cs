using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;
using Xunit;

namespace PaymentExecutionLambda.CancelLambda.ComponentTests;

internal record CancelRequestDetails(string Url, string Method, Dictionary<string, string> Headers, string? Body);
internal record GetPaymentIntentRequestDetails(string Url, string Method, Dictionary<string, string> Headers);

[Collection("Sequential")]
public class FunctionTests(LambdaTestFixture fixture) : IClassFixture<LambdaTestFixture>, IAsyncLifetime
{
    private readonly Function _function = fixture.CreateLambdaFunction();
    private readonly WiremockApi _wireMockApi = new("http://localhost:12112");
    private readonly List<Guid> _organisationIdsToCleanup = [];

    private const string CancelApiPathTemplate = "/v1/payments/cancel/{0}";
    private const string GetPaymentIntentApiPath = "/v1/payments/payment-intent";

    private const string HttpMethodPost = "POST";
    private const string HttpMethodGet = "GET";

    private const string XeroTenantIdHeader = "Xero-Tenant-Id";
    private const string XeroCorrelationIdHeader = "Xero-Correlation-Id";
    private const string AuthorizationHeader = "Authorization";

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var orgId in _organisationIdsToCleanup)
        {
            await fixture.DeletePaymentTransactionsByOrgAsync(orgId);
        }

        _organisationIdsToCleanup.Clear();

        await _wireMockApi.Reset();

        fixture.ResetFeatureFlagsToDefault();
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenValidCancelMessage_WhenProcessed_ThenShouldCancelWithStripeSuccessfully(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response);

        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");
        AssertGetPaymentIntentRequest(getPaymentIntentRequests.First(), organisationId, correlationId);

        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().HaveCount(1, "the Stripe cancel API should be called exactly once");

        var insertDto = new InsertPaymentTransactionDto
        {
            PaymentRequestId = paymentTransactionDto.PaymentRequestId,
            Status = paymentTransactionDto.Status,
            ProviderType = paymentTransactionDto.ProviderType,
            OrganisationId = organisationId
        };
        AssertCancelRequest(cancelRequests[0], insertDto, correlationId, cancelReason);
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenFeatureFlagDisabled_WhenProcessed_ThenShouldNotCallStripe(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        fixture.SetProviderCancellationFeatureFlag(false);
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should be processed without calling provider");

        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when feature flag is disabled");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenBatchWithOneFailure_WhenProcessed_ThenShouldReturnPartialBatchFailure(
        PaymentTransactionDto paymentTransactionDto1,
        PaymentTransactionDto paymentTransactionDto2,
        Guid correlationId1,
        Guid correlationId2)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;

        // Override specific field to trigger transient error
        paymentTransactionDto1.PaymentRequestId = Constants.StripeExeMockRequestIds.CancelTransientError;

        await SetupPaymentTransactionAsync(paymentTransactionDto1, organisationId);
        await SetupPaymentTransactionAsync(paymentTransactionDto2, organisationId);

        var messages = new[]
        {
            (
                messageBody: CreateCancelPaymentRequestJson(paymentTransactionDto1.PaymentRequestId),
                tenantId: organisationId.ToString(),
                correlationId: correlationId1.ToString()
            ),
            (
                messageBody: CreateCancelPaymentRequestJson(paymentTransactionDto2.PaymentRequestId),
                tenantId: organisationId.ToString(),
                correlationId: correlationId2.ToString()
            )
        };

        var sqsEvent = SqsHelpers.CreateSqsEventWithMultipleMessages(messages);
        var messageId1 = sqsEvent.Records[0].MessageId;

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        response.Should().NotBeNull();
        response.BatchItemFailures.Should().HaveCount(1, "only one message should fail");
        response.BatchItemFailures[0].ItemIdentifier.Should().Be(messageId1,
            "the first message (with 500 error) should be in batch failures");

        var cancelRequests1 = await GetCancelRequests(paymentTransactionDto1.PaymentRequestId);
        cancelRequests1.Should().HaveCount(4, "the failed request should have been attempted once and retried 3 times");

        var cancelRequests2 = await GetCancelRequests(paymentTransactionDto2.PaymentRequestId);
        cancelRequests2.Should().HaveCount(1, "the successful request should have been processed once");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenPaymentTransactionNotFound_WhenProcessed_ThenShouldSucceedWithoutRetry(
        Guid nonExistentPaymentRequestId,
        Guid tenantId,
        Guid correlationId)
    {
        // Arrange
        // Note: No payment transaction is inserted - simulating "not found" scenario
        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(nonExistentPaymentRequestId, tenantId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when payment transaction is not found to avoid unnecessary retries");

        var cancelRequests = await GetCancelRequests(nonExistentPaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when payment transaction does not exist");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenProviderStateTransientError_WhenProcessed_ThenShouldFailAndRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        // Use tenant ID that triggers 503 transient error from GetPaymentIntent
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusTransientError;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);
        var messageId = sqsEvent.Records[0].MessageId;

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertFailedResponse(response, messageId, "message should fail when GetPaymentIntent returns transient error (503) so it can be retried");

        // Verify GetPaymentIntent was called with correct headers
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");
        AssertGetPaymentIntentRequest(getPaymentIntentRequests.First(), organisationId, correlationId);

        // Cancel should not be called due to GetPaymentIntent failure
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when GetPaymentIntent fails");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenProviderStateNonTransientError_WhenProcessed_ThenShouldSucceedWithoutRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        // Use tenant ID that triggers 422 non-transient error from GetPaymentIntent
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusNonTransientError;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when GetPaymentIntent returns non-transient error (422) to avoid retries");

        // Verify GetPaymentIntent was called with correct headers
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");
        AssertGetPaymentIntentRequest(getPaymentIntentRequests.First(), organisationId, correlationId);

        // Cancel should not be called due to GetPaymentIntent failure
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when GetPaymentIntent fails");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenProviderStatusRequiresAction_WhenProcessed_ThenShouldSucceedWithoutCancelling(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        // Use tenant ID that triggers GetPaymentIntent success with requires_action status
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusRequiresAction;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when provider status is requires_action");

        // Verify GetPaymentIntent was called with correct headers
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");
        AssertGetPaymentIntentRequest(getPaymentIntentRequests.First(), organisationId, correlationId);

        // Cancel should NOT be called because provider status is requires_action
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when provider status is requires_action");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenInvalidProviderType_WhenProcessed_ThenShouldSucceedWithoutRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var messageBody = CreateCancelPaymentRequestJsonWithInvalidProvider(paymentTransactionDto.PaymentRequestId, cancelReason);
        var sqsEvent = SqsHelpers.CreateSqsEvent(
            messageBody,
            organisationId.ToString(),
            correlationId.ToString());

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should be deleted when provider type is invalid (non-retryable validation error)");

        // Verify no GetPaymentIntent call was made due to early validation failure
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().BeEmpty("GetPaymentIntent should not be called when provider type validation fails");

        // Verify no cancel call was made
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when provider type is invalid");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenAlreadyCancelledTransaction_WhenProcessed_ThenShouldSucceedIdempotently(
        PaymentTransactionDto paymentTransactionDto,
        Guid organisationId,
        Guid correlationId)
    {
        // Arrange
        paymentTransactionDto.Status = "Cancelled";
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed for already cancelled transaction (idempotent behavior)");

        // No GetPaymentIntent call should be made for already cancelled transactions
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().BeEmpty("GetPaymentIntent should not be called for already cancelled transaction");

        // No cancel call should be made
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made for already cancelled transaction");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenStripeCancelReturns404_WhenProcessed_ThenShouldSucceedWithoutRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        paymentTransactionDto.PaymentRequestId = Constants.StripeExeMockRequestIds.Cancel404NotFound;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when Stripe cancel returns 404 (payment doesn't exist at provider)");

        // Verify GetPaymentIntent was called
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");

        // Verify cancel was attempted and returned 404
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().HaveCount(1, "Stripe cancel should be attempted once");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenStripeCancelReturns4xxClientError_WhenProcessed_ThenShouldSucceedWithoutRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        paymentTransactionDto.PaymentRequestId = Constants.StripeExeMockRequestIds.Cancel4xxClientError;
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when Stripe cancel returns 4xx client error (non-retryable)");

        // Verify GetPaymentIntent was called
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().NotBeEmpty("GetPaymentIntent should be called");

        // Verify cancel was attempted and returned 4xx
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().HaveCount(1, "Stripe cancel should be attempted once");
    }

    [Theory, CancelLambdaAutoData]
    public async Task GivenPaymentNotCancellableStatus_WhenProcessed_ThenShouldSucceedWithoutRetry(
        PaymentTransactionDto paymentTransactionDto,
        Guid correlationId)
    {
        // Arrange
        var organisationId = Constants.StripeExeMockTenantIds.GetProviderStatusCancellable;
        paymentTransactionDto.Status = "Succeeded";
        await SetupPaymentTransactionAsync(paymentTransactionDto, organisationId);

        var cancelReason = "abandoned";
        var sqsEvent = CreateSqsEventForCancel(paymentTransactionDto.PaymentRequestId, organisationId, correlationId, cancelReason);

        // Act
        var response = await _function.Handler(sqsEvent);

        // Assert
        AssertSuccessfulResponse(response, "message should succeed when payment status is not cancellable (ValidationError - non-retryable)");

        // No GetPaymentIntent call should be made due to early validation failure
        var getPaymentIntentRequests = await GetPaymentIntentRequests(paymentTransactionDto.PaymentRequestId);
        getPaymentIntentRequests.Should().BeEmpty("GetPaymentIntent should not be called when payment status validation fails");

        // No cancel call should be made
        var cancelRequests = await GetCancelRequests(paymentTransactionDto.PaymentRequestId);
        cancelRequests.Should().BeEmpty("no Stripe cancel API call should be made when payment status is not cancellable");
    }

    // Helper Methods
    private async Task<List<CancelRequestDetails>> GetCancelRequests(Guid paymentRequestId)
    {
        var allRequests = await _wireMockApi.GetRequests();
        return allRequests
            .Where(r => r.Request?.Url?.Contains(string.Format(CancelApiPathTemplate, paymentRequestId)) == true)
            .Select(r => new CancelRequestDetails(
                r.Request?.Url ?? string.Empty,
                r.Request?.Method ?? string.Empty,
                r.Request?.Headers ?? new Dictionary<string, string>(),
                r.Request?.Body))
            .ToList();
    }

    private async Task<List<GetPaymentIntentRequestDetails>> GetPaymentIntentRequests(Guid paymentRequestId)
    {
        var allRequests = await _wireMockApi.GetRequests();
        return allRequests
            .Where(r => r.Request?.Url?.Contains(GetPaymentIntentApiPath) == true &&
                        r.Request?.Url?.Contains(paymentRequestId.ToString()) == true)
            .Select(r => new GetPaymentIntentRequestDetails(
                r.Request?.Url ?? string.Empty,
                r.Request?.Method ?? string.Empty,
                r.Request?.Headers ?? new Dictionary<string, string>()))
            .ToList();
    }

    private async Task SetupPaymentTransactionAsync(PaymentTransactionDto paymentTransactionDto, Guid organisationId)
    {
        _organisationIdsToCleanup.Add(organisationId);
        await fixture.InsertMockPaymentTransactionAsync(paymentTransactionDto, organisationId);
    }

    private static SQSEvent CreateSqsEventForCancel(Guid paymentRequestId, Guid organisationId, Guid correlationId, string cancelReason = "abandoned")
    {
        var messageBody = CreateCancelPaymentRequestJson(paymentRequestId, cancelReason);
        return SqsHelpers.CreateSqsEvent(
            messageBody,
            organisationId.ToString(),
            correlationId.ToString());
    }

    private static void AssertSuccessfulResponse(SQSBatchResponse response, string because = "all messages should be processed successfully")
    {
        response.Should().NotBeNull();
        response.BatchItemFailures.Should().BeEmpty(because);
    }

    private static void AssertFailedResponse(SQSBatchResponse response, string messageId, string because)
    {
        response.Should().NotBeNull();
        response.BatchItemFailures.Should().HaveCount(1, because);
        response.BatchItemFailures[0].ItemIdentifier.Should().Be(messageId);
    }

    private static void AssertRequestHeaders(Dictionary<string, string> headers, Guid expectedTenantId, Guid expectedCorrelationId)
    {
        headers.Should().ContainKey(XeroTenantIdHeader);
        headers[XeroTenantIdHeader].Should().Be(expectedTenantId.ToString());
        headers.Should().ContainKey(XeroCorrelationIdHeader);
        headers[XeroCorrelationIdHeader].Should().Be(expectedCorrelationId.ToString());
    }

    private static void AssertGetPaymentIntentRequest(GetPaymentIntentRequestDetails request, Guid expectedTenantId, Guid expectedCorrelationId)
    {
        AssertRequestHeaders(request.Headers, expectedTenantId, expectedCorrelationId);
    }

    private static string CreateCancelPaymentRequestJson(
        Guid paymentRequestId,
        string cancellationReason = "abandoned")
    {
        var request = new
        {
            paymentRequestId,
            providerType = "Stripe",
            cancellationReason
        };

        return JsonSerializer.Serialize(request);
    }

    private static string CreateCancelPaymentRequestJsonWithInvalidProvider(
        Guid paymentRequestId,
        string cancellationReason = "abandoned")
    {
        var request = new
        {
            paymentRequestId,
            providerType = "InvalidProvider",
            cancellationReason
        };

        return JsonSerializer.Serialize(request);
    }

    private static void AssertCancelRequest(CancelRequestDetails cancelRequest,
        InsertPaymentTransactionDto paymentTransactionDto, Guid correlationId,
        string cancelReason)
    {
        cancelRequest.Method.Should().Be(HttpMethodPost);
        cancelRequest.Url.Should().Contain(string.Format(CancelApiPathTemplate, paymentTransactionDto.PaymentRequestId));
        cancelRequest.Headers.Should().ContainKey(XeroTenantIdHeader)
            .WhoseValue.Should().Be(paymentTransactionDto.OrganisationId.ToString());
        cancelRequest.Headers.Should().ContainKey(XeroCorrelationIdHeader)
            .WhoseValue.Should().Be(correlationId.ToString());
        cancelRequest.Headers.Should().ContainKey(AuthorizationHeader)
            .WhoseValue.Should().StartWith("Bearer ");

        cancelRequest.Body.Should().NotBeNullOrEmpty("the request body should contain the cancellation reason");
        var requestBody = JsonSerializer.Deserialize<JsonElement>(cancelRequest.Body!);
        requestBody.TryGetProperty("cancellationReason", out var cancellationReasonElement).Should()
            .BeTrue("the body should contain cancellationReason field");
        var cancellationReason = cancellationReasonElement.GetString();
        cancellationReason.Should().NotBeNullOrEmpty("cancellationReason should have a value");
        cancellationReason.Should().Be(cancelReason, $"the default cancellation reason should be '{cancelReason}'");
    }
}
