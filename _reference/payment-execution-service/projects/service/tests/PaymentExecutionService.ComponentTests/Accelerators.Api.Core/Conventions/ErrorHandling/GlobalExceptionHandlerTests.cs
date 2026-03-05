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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Observability.Correlation;
using Xunit;

namespace Xero.Accelerators.Api.ComponentTests.Conventions.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task GivenAppInTestEnvWithProblemDetails_WhenNonExistentEndpointIsHit_ThenReturnsGenericJsonResponse()
    {
        // Arrange
        var exception = new Exception();
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ProblemDetailsFactory>(new MockProblemDetailsFactory());
                services.AddExceptionHandler<GlobalExceptionHandler>();
                services.AddProblemDetails();
            })
            .Configure(app =>
            {
                app.UseExceptionHandler();
                app.Run(_ => throw exception);
            }).UseEnvironment("Test"); ;

        var server = new TestServer(builder);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync("non-existent-route");

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent, serializerOptions);

        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        AssertProblemDetails(problemDetails);
    }

    [Fact]
    public async Task GivenAppInTestEnvWithProblemDetails_WhenEndpointThrowsException_ThenReturnsDefaultInstanceWithGeneratedCorrelationId()
    {
        // Arrange
        var routeThatThrows = "route-that-throws";
        var exception = new Exception();
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ProblemDetailsFactory>(new MockProblemDetailsFactory());
                services.AddRouting();
                services.AddExceptionHandler<GlobalExceptionHandler>();
                services.AddProblemDetails(opt =>
                    {
                        opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
                    });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseInboundXeroCorrelationIdMiddleware();
                app.UseExceptionHandler();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet(routeThatThrows, context => throw exception).AllowNoXeroCorrelationId();
                });
            }).UseEnvironment("Test");

        var server = new TestServer(builder);
        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(routeThatThrows);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseContent, serializerOptions);

        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        problemDetails!.Instance![0..^36].Should().Be("/route-that-throws/xero-correlation-id/");
        Guid.TryParse(problemDetails.Instance[^36..^0], out _).Should().BeTrue();
        AssertProblemDetails(problemDetails);
    }

    private static void AssertProblemDetails(ProblemDetails? problemDetails)
    {
        problemDetails!.Detail.Should().BeNull();
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        problemDetails.Title.Should().Be("An error occurred while processing your request.");
        problemDetails.Status.Should().Be(500);
    }
}
