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
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Conventions.Routing;

public class XeroTenantRouteDocumentFilterTests
{
    [Fact]
    public void XeroTenantRouteDocumentFilter_AddsDescriptionWithUuidSchema_ForXeroTenantIdParameter()
    {
        // Arrange
        var sut = new XeroTenantRouteDocumentFilter();
        var stubSwaggerDoc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["test"] = new()
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new()
                        {
                            Parameters = new List<OpenApiParameter>
                            {
                                new()
                                {
                                    Name = "xeroTenantId",
                                    In = ParameterLocation.Path,
                                    Schema = new OpenApiSchema()
                                }
                            }
                        }
                    }
                }
            }
        };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubSwaggerDoc.Paths["test"].Operations[OperationType.Get].Parameters[0].Description.Should().Be("Xero Tenant Id");
        stubSwaggerDoc.Paths["test"].Operations[OperationType.Get].Parameters[0].Schema.Format.Should().Be("uuid");
    }

    [Theory]
    [InlineData("", ParameterLocation.Header)]
    [InlineData("", ParameterLocation.Cookie)]
    [InlineData("test-parameter", ParameterLocation.Query)]
    [InlineData("test-parameter", ParameterLocation.Path)]
    [InlineData("xeroTenantId", ParameterLocation.Header)]
    [InlineData("xeroTenantId", ParameterLocation.Query)]
    [InlineData("xeroTenantId", ParameterLocation.Cookie)]
    public void XeroTenantRouteDocumentFilter_DoesNotAddDescription_ForNonXeroTenantIdParameter(string parameterName, ParameterLocation parameterLocation)
    {
        // Arrange
        var sut = new XeroTenantRouteDocumentFilter();
        var stubSwaggerDoc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["test"] = new()
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new()
                        {
                            Parameters = new List<OpenApiParameter>
                            {
                                new() { Name = parameterName, In = parameterLocation }
                            }
                        }
                    }
                }
            }
        };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubSwaggerDoc.Paths["test"].Operations[OperationType.Get].Parameters[0].Description.Should().NotBe("Xero Tenant Id");
    }
}
