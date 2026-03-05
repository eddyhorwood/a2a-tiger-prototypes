// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Observability.HealthChecks;
using Xero.Accelerators.Api.Core.OpenApi;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Observability.HealthChecks;

public class HealthChecksDocumentFilterTests
{
    [Fact]
    public void HealthChecksDocumentFilter_AddOperationsForHealthChecks()
    {
        // Arrange
        var stubEndpoint = CreateHealthCheckEndpoint("/healthcheck", "healthcheck");
        var stubEndpointDataSource = new DefaultEndpointDataSource(stubEndpoint);
        var stubDocCtx = Mock.Of<IOpenApiDocumentationContext>();
        var sut = new HealthChecksDocumentFilter(stubDocCtx, new List<EndpointDataSource> { stubEndpointDataSource });

        var stubSwaggerDoc = new OpenApiDocument { Paths = new OpenApiPaths() };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        var expected = new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    OperationId = "healthcheck",
                    Tags = new[] { new OpenApiTag { Name = "Health checks" } },
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new() { Description = "Healthy" },
                        ["500"] = new() { Description = "Unhealthy" },
                        ["503"] = new() { Description = "Degraded" }
                    }
                }
            }
        };
        stubSwaggerDoc.Paths["/healthcheck"].Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void HealthChecksDocumentFilter_AddErrorToOpenApiDocumentationContext_WhenHealthCheckDoesNotHaveRouteName()
    {
        // Arrange
        var stubEndpoint = CreateHealthCheckEndpoint("/healthcheck", string.Empty);
        var stubEndpointDataSource = new DefaultEndpointDataSource(stubEndpoint);
        var mockDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new HealthChecksDocumentFilter(mockDocCtx.Object, new List<EndpointDataSource> { stubEndpointDataSource });

        var stubSwaggerDoc = new OpenApiDocument { Paths = new OpenApiPaths() };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubSwaggerDoc.Paths.Should().BeEmpty();
        mockDocCtx.ShouldWarn(
            "Health check endpoint at route '{0}' is missing an `EndpointNameMetadata`.",
            "/healthcheck");
    }

    private static RouteEndpoint CreateHealthCheckEndpoint(string route, string endpointName)
    {
        var metadata = new List<object>
        {
            new HealthCheckMetadata(
            new Dictionary<HealthStatus, int>(new HealthCheckOptions().ResultStatusCodes)
            {
                [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable
            }
        )
        };
        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            metadata.Add(new EndpointNameMetadata(endpointName));
        }

        return new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(route),
            order: 0,
            new EndpointMetadataCollection(metadata),
            displayName: "Health checks"
        );
    }
}
