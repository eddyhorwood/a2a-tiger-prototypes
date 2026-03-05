using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Common;
using PaymentExecutionService.ComponentTests.HttpClientExtensions;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
using Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;
using Xunit;

namespace PaymentExecutionService.ComponentTests.Middleware;

public class ServiceFeatureFlagFilterTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>
{
    private readonly JsonSerializerOptions _serializerOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client = fixture
        .CreateAuthenticatedClientWithTenantId(PaymentExecutionService.Constants.ServiceAuthorizationScopes
            .Submit)
        .WithXeroUserId(Constants.UserInformation.UserIdAdmin)
        .WithXeroCorrelationId()
        .WithProviderAccountId();
    private readonly HttpClient _unauthenticatedClient = fixture.CreateUnauthenticatedClient();
    private readonly HttpClient _clientNoCorrelationId = fixture
        .CreateAuthenticatedClientWithTenantId(PaymentExecutionService.Constants.ServiceAuthorizationScopes
            .Submit);


    [Fact]
    public async Task GivenServiceToggleEnabled_WhenInvoked_ReturnsOk()
    {
        // Arrange
        var featureFlagDataSource = fixture.GetServerFeatureServiceDataSource();
        var serviceFlag = ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled;
        featureFlagDataSource.Update(featureFlagDataSource
            .Flag(serviceFlag.Name).VariationForAll(true));

        // Act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment,
            MakeSubmitPaymentTransactionRequestBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenServiceToggleDisabled_WhenBusinessFunctionalEndpointInvoked_Returns503Error()
    {
        var featureFlagDataSource = fixture.GetServerFeatureServiceDataSource();
        var serviceFlag = ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled;
        featureFlagDataSource.Update(featureFlagDataSource
            .Flag(serviceFlag.Name).VariationForAll(false));
        // Arrange
        var expectedProblemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Type = "https://common.service.xero.com/schema/problems/payment-execution-service-disabled",
            Title = "The payment execution service is currently disabled.",
            Detail = "The payment execution service is currently disabled.",
        };

        // Act
        var response = await _client.PostAsync(Constants.Endpoints.SubmitStripePayment,
            MakeSubmitPaymentTransactionRequestBody());
        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, _serializerOpts);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        problemDetails!.Detail.Should().Be(expectedProblemDetails.Detail);
        problemDetails.Type.Should().Be(expectedProblemDetails.Type);
        problemDetails.Title.Should().Be(expectedProblemDetails.Title);
    }

    [Fact]
    public async Task GivenServiceToggleDisabled_WhenHealthCheckInvoked_ReturnsOk()
    {
        var featureFlagDataSource = fixture.GetServerFeatureServiceDataSource();
        var serviceFlag = ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled;
        featureFlagDataSource.Update(featureFlagDataSource
            .Flag(serviceFlag.Name).VariationForAll(false));
        // Arrange

        // Act
        var response = await _client.GetAsync("/healthcheck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenServiceToggleDisabled_WhenPingInvoked_ReturnsOk()
    {
        var featureFlagDataSource = fixture.GetServerFeatureServiceDataSource();
        var serviceFlag = ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled;
        featureFlagDataSource.Update(featureFlagDataSource
            .Flag(serviceFlag.Name).VariationForAll(false));
        // Arrange

        // Act
        var response = await _client.GetAsync("/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static StringContent MakeSubmitPaymentTransactionRequestBody()
    {
        var requestObj = new SubmitStripeRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            PaymentMethodId = "some-method"
        };
        return new StringContent(JsonSerializer.Serialize(requestObj), Encoding.UTF8, "application/json");
    }
}
