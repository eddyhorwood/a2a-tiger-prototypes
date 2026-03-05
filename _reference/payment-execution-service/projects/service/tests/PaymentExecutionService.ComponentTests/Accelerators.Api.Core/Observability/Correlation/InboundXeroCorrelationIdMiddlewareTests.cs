// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xero.Accelerators.Api.ComponentTests.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Observability.Correlation;
using Xero.Accelerators.Api.Core.Observability.Logging;
using Xunit;

namespace Xero.Accelerators.Api.ComponentTests.Observability.Correlation;

public class InboundXeroCorrelationIdMiddlewareTests
{
    private static readonly string _routeCorrelationIdRequired = "/required";
    private static readonly string _routeCorrelationIdNotRequired = "/not-required";

    private readonly TestServer _testServer = new(
        new WebHostBuilder()
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ProblemDetailsFactory>(new MockProblemDetailsFactory());
                services.AddRouting();
                services.AddProblemDetails(opt =>
                    {
                        opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
                    });
            })
            .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseInboundXeroCorrelationIdMiddleware();
                        app.UseRequestLogging();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet(_routeCorrelationIdRequired, () => "xyz");
                            endpoints.MapGet(_routeCorrelationIdNotRequired, () => "xyz").WithMetadata(new AllowNoXeroCorrelationIdAttribute());
                        });
                    })
        );

    [Fact]
    public async Task GivenEndpointWithInboundXeroCorrelationIdRequired_WhenTheRequestHasValidCorrelationId_ThenReturns200()
    {
        //Arrange
        var client = _testServer.CreateClient().WithXeroCorrelationId();

        // Act
        var response = await client.GetAsync(_routeCorrelationIdRequired);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenEndpointWithInboundXeroCorrelationIdRequired_WhenTheRequestHasNoCorrelationId_ThenReturnsBadRequest()
    {
        // Arrange
        var client = _testServer.CreateClient();

        var expectedProblemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://common.service.xero.com/schema/problems/correlation-id-not-found",
            Title = "The Xero-Correlation-Id header is required.",
            Detail = "The Xero-Correlation-Id header is required.",
            Instance = ""
        };

        // Act
        var response = await client.GetAsync(_routeCorrelationIdRequired);
        var content = await response.Content.ReadAsStringAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, serializerOptions);

        // Assert
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Fact]
    public async Task GivenEndpointWithInboundXeroCorrelationIdRequired_WhenTheRequestHasEmptyCorrelationId_ThenReturns500()
    {
        // Arrange
        var client = _testServer.CreateClient().WithXeroCorrelationId(Guid.Empty.ToString());

        var expectedProblemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://common.service.xero.com/schema/problems/correlation-id-is-empty",
            Title = "The Xero-Correlation-Id header should not be an empty GUID.",
            Detail = "The Xero-Correlation-Id header should not be an empty GUID.",
            Instance = "/required/xero-correlation-id/00000000-0000-0000-0000-000000000000"
        };

        // Act
        var response = await client.GetAsync(_routeCorrelationIdRequired);
        var content = await response.Content.ReadAsStringAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, serializerOptions);

        // Assert
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Fact]
    public async Task GivenEndpointWithInboundXeroCorrelationIdRequired_WhenTheRequestHasInvalidCorrelationId_ThenReturns500()
    {
        // Arrange
        var client = _testServer.CreateClient().WithXeroCorrelationId("some non guid value");

        var expectedProblemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://common.service.xero.com/schema/problems/correlation-id-not-valid",
            Title = "The Xero-Correlation-Id header should be a valid GUID.",
            Detail = "The Xero-Correlation-Id header should be a valid GUID.",
            Instance = "/required/xero-correlation-id/some non guid value"
        };

        // Act
        var response = await client.GetAsync(_routeCorrelationIdRequired);
        var content = await response.Content.ReadAsStringAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, serializerOptions);

        // Assert
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Fact]
    public async Task GivenEndpointWithInboundXeroCorrelationIdNotRequired_WhenTheRequestHasNoCorrelationId_ThenReturns200()
    {
        // Arrange
        var client = _testServer.CreateClient();

        // Act
        var response = await client.GetAsync(_routeCorrelationIdNotRequired);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
