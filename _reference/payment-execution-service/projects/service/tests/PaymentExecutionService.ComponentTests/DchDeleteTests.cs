using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Repository.Models;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xunit;
using static System.Net.Mime.MediaTypeNames.Application;
using static PaymentExecutionService.Constants.ServiceAuthorizationScopes;

namespace PaymentExecutionService.ComponentTests;

public class DchDeleteTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>
{
    // Note; this explicitly does NOT have a XeroCorrelationId or XeroTenantId header, since these are unlikely to be
    // sent from the DCH component which calls our endpoint.
    private readonly HttpClient _client = fixture
        .CreateAuthenticatedClientWithTenantId(DchDelete);

    private readonly JsonSerializerOptions _serializerOpts = new() { PropertyNameCaseInsensitive = true };
    private const string Path = "/v1/payments/dch-delete";

    [Fact]
    public async Task GivenIncorrectAuthScope_WhenDchDeleteCalled_ThenForbiddenReturned()
    {
        // Arrange
        DchDeletePayload payload = new() { IdToDelete = Guid.NewGuid() };
        var incorrectScopeClient = fixture.CreateAuthenticatedClientWithTenantId(Submit);
        var serialisedPayload = new StringContent(JsonSerializer.Serialize(payload, _serializerOpts), Encoding.UTF8,
            Json);

        // Act
        var response = await incorrectScopeClient.PostAsync(Path, serialisedPayload);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenInvalidRequestBody_WhenDchDeleteCalled_ThenBadRequestReturned()
    {
        // Arrange
        var payload = new StringContent("{\"incorrect_key\": \"not-a-guid\"}", Encoding.UTF8, mediaType: Json);

        // Act
        var response = await _client.PostAsync(Path, payload);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            await response.Content.ReadAsStringAsync(), _serializerOpts
        );
        problemDetails!.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.ExtractErrorDetailsJsonString().Should()
            .Contain("missing required properties, including the following: idToDelete");
    }

    [Fact]
    public async Task GivenValidRequestBody_WhenOrgNotRecognised_ThenNotFoundReturned()
    {
        // Arrange
        var payload = new DchDeletePayload { IdToDelete = Guid.NewGuid() };
        var serialisedPayload =
            new StringContent(JsonSerializer.Serialize(payload, _serializerOpts), Encoding.UTF8, Json);

        // Act
        var response = await _client.PostAsync(Path, serialisedPayload);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenValidRequestBody_WhenOrgRecognised_ThenRowsDeletedAndOkResponseIsReturned()
    {
        // Arrange
        // n.b; Not cleaning up this resource in DisposeAsync, since this is deleted during the test.
        var paymentTransactionDtoToBeInserted = new InsertPaymentTransactionDto
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            Status = "Pending",
            OrganisationId = Guid.NewGuid()
        };

        await fixture.InsertPaymentTransactionIfNotExist(paymentTransactionDtoToBeInserted);
        var payload = new DchDeletePayload { IdToDelete = paymentTransactionDtoToBeInserted.OrganisationId };
        var serialisedPayload = new StringContent(JsonSerializer.Serialize(payload, _serializerOpts), Encoding.UTF8,
            Json);

        // Act
        var response = await _client.PostAsync(Path, serialisedPayload);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeatDeleteResponse = await _client.PostAsync(Path, serialisedPayload);
        response.Should().NotBeNull();
        repeatDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenValidTokenWithNonWhitelistedClientId_WhenDchDelete_ThenReturnsForbidden()
    {
        // Arrange
        var payload = new DchDeletePayload { IdToDelete = Guid.NewGuid() };
        var serialisedPayload = new StringContent(JsonSerializer.Serialize(payload, _serializerOpts), Encoding.UTF8,
            Json);
        var clientWithNonWhitelistedClientId =
            fixture.CreateAuthenticatedClientWithClientId("local_caller_non-whitelisted", DchDelete)
                .WithXeroCorrelationId()
                .WithXeroTenantId();

        // Act
        var response = await clientWithNonWhitelistedClientId.PostAsync(Path, serialisedPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
