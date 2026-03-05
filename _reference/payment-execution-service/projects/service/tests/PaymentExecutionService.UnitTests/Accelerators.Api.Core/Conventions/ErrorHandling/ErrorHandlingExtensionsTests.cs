// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xero.Accelerators.Api.Core;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Conventions.ErrorHandling;

public class ErrorHandlingExtensionsTests
{
    private readonly MockProblemDetailsFactory _factory = new();
    [Fact]
    public void GivenCustomizeXeroProblemDetails_WhenExceptionContainsProblemDetailsIdentifier_ReturnsProblemDetailsContextWithCustomType()
    {
        // Arrange
        var exception = new Exception();
        exception.Data.Add(Constants.ExceptionDataFields.ProblemDetailsIdentifier, "custom-exception-identifier");

        var context = new DefaultHttpContext();
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature() { Error = exception });

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
        };

        // Act
        var customizeAction = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        customizeAction.Invoke(problemDetailsContext);

        // Assert
        problemDetailsContext.ProblemDetails.Should().BeEquivalentTo(
            new ProblemDetails
            {
                Type = "https://common.service.xero.com/schema/problems/custom-exception-identifier"
            }
        );
    }

    [Fact]
    public void GivenCustomizeXeroProblemDetails_WhenGenerateProblemInstanceExists_ReturnsProblemDetailsContextWithCustomProblemDetailsInstance()
    {
        // Arrange
        var options = new CustomProblemDetailsOptions
        {
            GenerateProblemInstance = (httpContext, exception) => "a-custom-instance"
        };

        var context = new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext(),
            Exception = new Exception()
        };

        // Act
        var customizeAction = ErrorHandlingExtensions.CustomizeXeroProblemDetails(options);
        customizeAction.Invoke(context);

        // Assert
        context.ProblemDetails.Should().BeEquivalentTo(
            new ProblemDetails
            {
                Instance = "a-custom-instance"
            }
        );
    }

    [Fact]
    public void GivenCustomizeXeroProblemDetails_WhenXeroCorrelationIdHeaderExists_ReturnsProblemDetailsContextWithCorrelationIdInstance()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Xero-Correlation-Id", "123456");

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = new Exception(),

        };
        // Act
        var customizeAction = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        customizeAction.Invoke(context);

        // Assert
        context.ProblemDetails.Should().BeEquivalentTo(new ProblemDetails
        {
            Instance = "/xero-correlation-id/123456"
        });
    }

    [Fact]
    public void GivenCustomizeXeroProblemDetails_WithNoOptionsOrHeader_ReturnsEmptyProblemDetails()
    {
        // Arrange
        var context = new ProblemDetailsContext
        {
            HttpContext = new DefaultHttpContext(),
            Exception = new Exception()
        };
        // Act
        var customizeAction = ErrorHandlingExtensions.CustomizeXeroProblemDetails();
        customizeAction.Invoke(context);

        // Assert
        context.ProblemDetails.Should().BeEquivalentTo(new ProblemDetails());
    }

    [Fact]
    public void GivenCreateCustomProblem_WhenCustomInstanceExists_ThenReturnsProblemDetailsWithCustomInstance()
    {
        // Arrange 
        var context = CreateHttpContext("a-MixedCASE-UuID");
        var customInstance = "Custom identifier of a problem instance";

        // Act
        var problemDetails = _factory.CreateCustomProblem(
            context,
            "invoice-not-found",
            StatusCodes.Status404NotFound,
            customInstance,
            "Invoice with ID {InvoiceId} was not found.",
            new object[] { "12345" }
        );

        // Assert
        problemDetails.Should().BeEquivalentTo(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://a-mixedcase-uuid.service.xero.com/schema/problems/invoice-not-found",
            Title = "Invoice with ID {InvoiceId} was not found.",
            Detail = "Invoice with ID 12345 was not found.",
            Instance = "Custom identifier of a problem instance"
        });
    }

    [Fact]
    public void GivenCreateCustomProblem_WhenCorrelationIdExists_ThenReturnsProblemDetailsWithCorrelatedInstance()
    {
        // Arrange
        var context = CreateHttpContext("a-MixedCASE-UuID");
        context.Request.Headers.Append("Xero-Correlation-Id", "Testing-Correlation-Id");
        context.Request.Path = "/a-route";

        // Act
        var problemDetails = _factory.CreateCustomProblem(
            context,
            "invoice-not-found",
            StatusCodes.Status404NotFound,
            "Invoice with ID {InvoiceId} was not found.",
            new object[] { "12345" }
        );

        // Assert
        problemDetails.Should().BeEquivalentTo(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://a-mixedcase-uuid.service.xero.com/schema/problems/invoice-not-found",
            Title = "Invoice with ID {InvoiceId} was not found.",
            Detail = "Invoice with ID 12345 was not found.",
            Instance = "/a-route/xero-correlation-id/testing-correlation-id"
        });
    }

    [Fact]
    public void GivenCreateCommonProblem_WhenCorrelationIdDoesNotExist_ThenReturnsProblemDetailsWithEmptyInstance()
    {
        // Act
        var problemDetails = _factory.CreateCommonProblem(
            new DefaultHttpContext(),
            "invoice-not-found",
            StatusCodes.Status404NotFound,
            "Invoice with ID {InvoiceId} was not found.",
            new object[] { "12345" }
        );

        // Assert
        problemDetails.Should().BeEquivalentTo(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://common.service.xero.com/schema/problems/invoice-not-found",
            Title = "Invoice with ID {InvoiceId} was not found.",
            Detail = "Invoice with ID 12345 was not found.",
            Instance = ""
        });
    }

    private static HttpContext CreateHttpContext(string componentUuid)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CatalogueMetadata
        (
            Name: It.IsAny<string>(),
            Description: It.IsAny<string>(),
            ComponentUuid: componentUuid,
            EnvironmentUrls: It.IsAny<Dictionary<string, string>>(),
            ApiType: It.IsAny<XeroApiType>()
        ));

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            RequestServices = services.BuildServiceProvider()
        };

        return context;
    }
}
