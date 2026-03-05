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
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class BadRequestDocumentFilterTests
{
    [Fact]
    public void GivenApply_WhenRequestsConsumesData_Add400Response()
    {
        // Arrange
        var testRoute = "/test-route";
        var stubSwaggerDoc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                [testRoute] = new()
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new()
                        {
                            Parameters = new List<OpenApiParameter>
                            {
                                new() { Name = "test-param", In = ParameterLocation.Path }
                            }
                        },
                        [OperationType.Put] = new()
                        {
                            Parameters = new List<OpenApiParameter>
                            {
                                new() { Name = "test-param", In = ParameterLocation.Query }
                            }
                        },
                        [OperationType.Post] = new()
                        {
                            RequestBody = new OpenApiRequestBody()
                        },
                        [OperationType.Delete] = new()
                    }
                }
            }
        };
        var stubContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());
        var operations = stubSwaggerDoc.Paths[testRoute].Operations;
        var expectedOperations = new Dictionary<OperationType, OpenApiOperation>
        {
            [OperationType.Get] = new()
            {
                Parameters = new List<OpenApiParameter>
                {
                    new() { Name = "test-param", In = ParameterLocation.Path }
                },
                Responses = new OpenApiResponses
                {
                    ["400"] = new() { Description = "Bad Request" }
                },
            },
            [OperationType.Put] = new()
            {
                Parameters = new List<OpenApiParameter>
                {
                    new() { Name = "test-param", In = ParameterLocation.Query }
                },
                Responses = new OpenApiResponses
                {
                    ["400"] = new() { Description = "Bad Request" }
                },
            },
            [OperationType.Post] = new()
            {
                RequestBody = new OpenApiRequestBody(),
                Responses = new OpenApiResponses
                {
                    ["400"] = new() { Description = "Bad Request" }
                },
            },
            [OperationType.Delete] = new()
        };

        // Act
        var sut = new BadRequestDocumentFilter();
        sut.Apply(stubSwaggerDoc, stubContext);

        // Assert
        operations.Should().BeEquivalentTo(expectedOperations);
    }
}
