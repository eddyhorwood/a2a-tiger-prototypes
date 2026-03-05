using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PaymentExecution.Domain.Queries;
using PaymentExecution.Repository.Models;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;
namespace PaymentExecutionService.ComponentTests;

[Collection("NoParallelizationCollection")]
public class GetPaymentTransactionTests : IClassFixture<ComponentTestsFixture>, IDisposable
{
    private readonly ComponentTestsFixture _fixture;
    private readonly HttpClient _client;
    private const string GetPaymentTransactionScope =
        PaymentExecutionService.Constants.ServiceAuthorizationScopes.ReadOnly;

    public GetPaymentTransactionTests(ComponentTestsFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClientWithTenantId(GetPaymentTransactionScope)
            .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
            .WithXeroCorrelationId();
    }

    [Fact]
    public async Task GivenValidAuthorizedRequest_WhenGetPaymentTransactionEndpoint_ThenReturnsPaymentTransactionWith200()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        var paymentTransactionDtoToBeInserted = new InsertPaymentTransactionDto
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = "Stripe",
            Status = "Pending",
            OrganisationId = Guid.NewGuid()
        };

        await _fixture.InsertPaymentTransactionIfNotExist(paymentTransactionDtoToBeInserted);

        // Act
        var response = await _client.GetAsync(Constants.Endpoints.GetPaymentTransaction + paymentRequestId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnPaymentTransaction = await response.Content.ReadFromJsonAsync<GetPaymentTransactionQueryResponse>();
        returnPaymentTransaction.Should().NotBeNull();
        returnPaymentTransaction!.PaymentRequestId.Should().Be(paymentRequestId);
        returnPaymentTransaction.ProviderServiceId.Should().BeNull();
        returnPaymentTransaction.PaymentProviderPaymentReferenceId.Should().BeNull();
        returnPaymentTransaction.PaymentProviderPaymentTransactionId.Should().BeNull();
        returnPaymentTransaction.Status.Should().Be(paymentTransactionDtoToBeInserted.Status);
        returnPaymentTransaction.ProviderType.Should().Be(paymentTransactionDtoToBeInserted.ProviderType);
        returnPaymentTransaction.Fee.Should().BeNull();
        returnPaymentTransaction.FeeCurrency.Should().BeNull();
        returnPaymentTransaction.PaymentTransactionId.Should().NotBeEmpty();
        returnPaymentTransaction.UpdatedUtc.Should().NotBeNull();
        returnPaymentTransaction.CreatedUtc.Should().NotBeNull();
        returnPaymentTransaction.CancellationReason.Should().BeNull();
        returnPaymentTransaction.FailureDetails.Should().BeNull();
    }

    [Fact]
    public async Task GivenUsingInvalidAccessToken_WhenGetPaymentTransactionEndpoint_ThenReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization
            = new AuthenticationHeaderValue("Bearer", "Invalid token");

        // Act
        var response = await _client.GetAsync(Constants.Endpoints.GetPaymentTransaction + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenUsingValidAccessTokenWithNonWhitelistedClientId_WhenGetPaymentTransaction_ThenReturnsForbidden()
    {
        // Arrange
        var clientWithNonWhitelistedClientId =
            _fixture.CreateAuthenticatedClientWithClientId("local_caller_non-whitelisted", GetPaymentTransactionScope)
                .WithXeroCorrelationId()
                .WithXeroTenantId();

        // Act
        var response = await clientWithNonWhitelistedClientId.GetAsync(Constants.Endpoints.GetPaymentTransaction + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public void Dispose()
    {
        _fixture.WipePaymentTransactionDb().Wait();
        GC.SuppressFinalize(this);
    }
}
