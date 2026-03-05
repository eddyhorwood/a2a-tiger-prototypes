// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.Common;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Conventions.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private readonly static MockProblemDetailsFactory _factory = new();

    [Fact]
    public async Task GivenGlobalExceptionHandler_WhenStandardExceptionIsThrown_ThenReturnsGenericJsonResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        });
        services.AddOptions();
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() }, RequestServices = serviceProvider };

        var handler = new GlobalExceptionHandler(_factory);
        var exception = new Exception();

        // Act
        await handler.TryHandleAsync(context, exception, new CancellationTokenSource().Token);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);
        problemDetails.Should().BeEquivalentTo(
            new ProblemDetailsExtended
            {
                Instance = null,
                Detail = null,
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                ErrorCode = ErrorConstants.ErrorCode.ExecutionUnexpectedError,
                ProviderErrorCode = null
            }
        );
    }

    [Fact]
    public async Task GivenGlobalExceptionHandler_WhenStandardExceptionIsThrownFromSubmitPath_ThenReturnsGenericJsonResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        });
        services.AddOptions();
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() }, RequestServices = serviceProvider };
        context.Request.Path = $"/{PaymentExecutionService.Constants.RouteConstants.SubmitStripePayment}";
        var handler = new GlobalExceptionHandler(_factory);
        var exception = new Exception();

        // Act
        await handler.TryHandleAsync(context, exception, new CancellationTokenSource().Token);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);
        problemDetails.Should().BeEquivalentTo(
            new ProblemDetailsExtended
            {
                Instance = null,
                Detail = null,
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                ErrorCode = ErrorConstants.ErrorCode.ExecutionSubmitError,
                ProviderErrorCode = null
            }
        );
    }

    [Fact]
    public async Task GivenGlobalExceptionHandler_When400IsIsThrownWithInfo_ThenReturnsProblemDetailsJsonResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        });
        services.AddOptions();
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() }, RequestServices = serviceProvider };

        var handler = new GlobalExceptionHandler(_factory);
        var exception = new BadHttpRequestException("");

        // Act
        await handler.TryHandleAsync(context, exception, new CancellationTokenSource().Token);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);

        problemDetails.Should().BeEquivalentTo(
            new ProblemDetailsExtended()
            {
                Instance = null,
                Detail = null,
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                ErrorCode = ErrorConstants.ErrorCode.ExecutionUnexpectedError,
                ProviderErrorCode = null
            }
        );
    }

    [Fact]
    public async Task GivenGlobalExceptionHandler_When400IsIsThrownWithInfoFromSubmitPath_ThenReturnsProblemDetailsJsonResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        });
        services.AddOptions();
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() }, RequestServices = serviceProvider };
        context.Request.Path = $"/{PaymentExecutionService.Constants.RouteConstants.SubmitStripePayment}";
        var handler = new GlobalExceptionHandler(_factory);
        var exception = new BadHttpRequestException("");

        // Act
        await handler.TryHandleAsync(context, exception, new CancellationTokenSource().Token);

        // Assert
        var problemDetails = await ReadProblemDetailsAsync(context);

        problemDetails.Should().BeEquivalentTo(
            new ProblemDetailsExtended()
            {
                Instance = null,
                Detail = null,
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                ErrorCode = ErrorConstants.ErrorCode.ExecutionSubmitError,
                ProviderErrorCode = null
            }
        );
    }

    private static async Task<ProblemDetailsExtended?> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var stringReader = new StreamReader(context.Response.Body);
        var body = await stringReader.ReadToEndAsync();

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsExtended>(body, serializerOptions);
        return problemDetails;
    }
}
