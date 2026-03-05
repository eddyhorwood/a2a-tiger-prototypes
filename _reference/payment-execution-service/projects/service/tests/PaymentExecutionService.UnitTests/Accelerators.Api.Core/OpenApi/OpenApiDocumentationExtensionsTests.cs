// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class OpenApiDocumentationExtensionsTests
{
    [Fact]
    public void FindEndpointForOperation_WhenLookingForOperationInRouteEndpointCollection_ReturnsCorrectEndpoint()
    {
        // Arrange
        var operation = new OpenApiOperation { OperationId = "TestOperationId" };
        var expectedEndpoint = OpenApiTestsHelpers.CreateRouteEndpoint("TestOperationId");

        // Act
        var actual = operation.FindEndpointForOperation(new List<RouteEndpoint>
        {
            OpenApiTestsHelpers.CreateRouteEndpoint("SomeOtherOperationId"), expectedEndpoint
        });

        // Assert
        actual.Should().Be(expectedEndpoint);
    }

    [Fact]
    public void FindEndpointForOperation_WhenEndpointDoesNotExist_ReturnsNull()
    {
        // Arrange
        var operation = new OpenApiOperation { OperationId = "TestOperationId" };

        // Act
        var actual = operation.FindEndpointForOperation(new List<RouteEndpoint>
        {
            OpenApiTestsHelpers.CreateRouteEndpoint("NonExistentOperationId")
        });

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void FindEndpointForOperation_WhenLookingForOperationInEndpointDataSource_ReturnsCorrectEndpoint()
    {
        // Arrange
        var expectedEndpoint = OpenApiTestsHelpers.CreateRouteEndpoint("TestOperationId");
        var operation = new OpenApiOperation { OperationId = "TestOperationId" };
        var routeEndpointDataSource = new EndpointDataSource[]
        {
            new DefaultEndpointDataSource(new List<RouteEndpoint> { expectedEndpoint })
        };

        // Act
        var actual = operation.FindEndpointForOperation(routeEndpointDataSource);

        // Assert
        actual.Should().Be(expectedEndpoint);
    }

    [Fact]
    public void FindOperationForRoute_WhenLookingForOperationBasedOnOperationId_ReturnsCorrectOperation()
    {
        // Arrange
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("TestOperationId");
        var expectedOperation = new OpenApiOperation { OperationId = "TestOperationId" };
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            { { "test-route", new OpenApiPathItem
            {
                Operations =
                {
                    [OperationType.Get] = expectedOperation,
                    [OperationType.Post] = new OpenApiOperation { OperationId = "AnotherTestOperationId" }
                }
            } } }
        };

        // Act
        var actual = sut.FindOperationForRoute(endpoint);

        // Assert
        actual.Should().BeEquivalentTo(expectedOperation);
    }

    [Fact]
    public void FindOperationForRoute_WhenEndpointDoesNotHaveName_ReturnsNull()
    {
        // Arrange
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint(string.Empty);
        var sut = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            { { "test-route", new OpenApiPathItem
            {
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation { OperationId = "TestOperationId" }
                }
            } } }
        };

        // Act
        var actual = sut.FindOperationForRoute(endpoint);

        // Assert
        actual.Should().BeNull();
    }
}
