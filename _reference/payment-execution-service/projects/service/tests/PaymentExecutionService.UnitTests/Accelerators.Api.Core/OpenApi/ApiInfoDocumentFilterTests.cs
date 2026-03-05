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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class ApiInfoDocumentFilterTests
{
    [Fact]
    public void
        GivenApiInfoDocumentFilter_WhenCatalogueMetadataIsValid_ThenAddsInfoForOpenApiSpec()
    {
        // Arrange
        var stubCatalogueMetadata = new CatalogueMetadata
        (
            Name: "test-api",
            ApiType: XeroApiType.Product,
            ComponentUuid: "abcdef123",
            Description: "test-description",
            EnvironmentUrls: new Dictionary<string, string>
            {
                ["Uat"] = "https://test.uat.com",
                ["Production"] = "https://test.prod.com"
            }
        );
        var stubDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut =
            new ApiInfoDocumentFilter(stubCatalogueMetadata, stubDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument { Info = new OpenApiInfo() };
        var stubFilterCtx = new DocumentFilterContext(
            Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(),
            new SchemaRepository());
        var expectedSwaggerDoc = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "test-api",
                Extensions =
                    new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-xero-api-type"] =
                            new OpenApiString("Product")
                    },
                Contact =
                    new OpenApiContact
                    {
                        Url = new Uri(
                            "https://app.getcortexapp.com/admin/service?tenantCode=Xero&tag=abcdef123/")
                    },
                Description = "test-description"
            },
            Servers = new List<OpenApiServer>
            {
                new()
                {
                    Description = "Uat URL",
                    Url = "https://test.uat.com"
                },
                new()
                {
                    Description = "Production URL",
                    Url = "https://test.prod.com"
                }
            }
        };

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubSwaggerDoc.Should().BeEquivalentTo(expectedSwaggerDoc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("        ")]
    public void GivenApiInfoDocumentFilter_WhenNameIsEmpty_ThenAddErrorToOpenApiDocumentationContext(string invalidName)
    {
        // Arrange
        var stubCatalogueMetadata = new CatalogueMetadata
        (
            Name: invalidName,
            ApiType: XeroApiType.Product,
            ComponentUuid: "abcdef123",
            Description: "test-description",
            EnvironmentUrls: new Dictionary<string, string>
            {
                ["Uat"] = "https://test.uat.com",
                ["Production"] = "https://test.prod.com"
            }
        );
        var stubDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new ApiInfoDocumentFilter(stubCatalogueMetadata, stubDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument { Info = new OpenApiInfo() };
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubDocumentFilterContext);

        // Assert
        stubDocCtx.ShouldWarn(
            "OpenAPI spec Title cannot be empty. Please configure a `Name` for `CatalogueMetadata` in `Program.cs`.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("       ")]
    public void GivenApiInfoDocumentFilter_WhenDescriptionIsEmpty_ThenAddErrorToOpenApiDocumentationContext(string invalidDescription)
    {
        var stubCatalogueMetadata = new CatalogueMetadata
        (
            Name: "test-api",
            ApiType: XeroApiType.Product,
            ComponentUuid: "abcdef123",
            Description: invalidDescription,
            EnvironmentUrls: new Dictionary<string, string>
            {
                ["Uat"] = "https://test.uat.com",
                ["Production"] = "https://test.prod.com"
            }
        );
        var stubDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new ApiInfoDocumentFilter(stubCatalogueMetadata, stubDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument { Info = new OpenApiInfo() };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubDocCtx.ShouldWarn(
            "OpenAPI spec Description cannot be empty. Please configure a `Description` for `CatalogueMetadata` in `Program.cs`.");
    }

    [Fact]
    public void GivenApiInfoDocumentFilter_WhenEnvironmentUrlsAreEmpty_ThenAddErrorToOpenApiDocumentationContext()
    {
        // Arrange
        var stubCatalogueMetadata = new CatalogueMetadata
        (
            Name: "test-api",
            ApiType: XeroApiType.Product,
            ComponentUuid: "abcdef123",
            Description: "test-description",
            EnvironmentUrls: new Dictionary<string, string>()
        );
        var stubDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new ApiInfoDocumentFilter(stubCatalogueMetadata, stubDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument { Info = new OpenApiInfo() };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        stubDocCtx.ShouldWarn(
            "OpenAPI spec is missing Server URLs. Please configure `EnvironmentUrls` for `CatalogueMetadata` in `Program.cs`.");
    }

    [Fact]
    public void GivenApiInfoDocumentFilter_WhenEnvironmentUrlIsInvalid_ThenAddErrorToOpenApiDocumentationContext()
    {
        // Arrange
        var stubCatalogueMetadata = new CatalogueMetadata
        (
            Name: "test-api",
            ApiType: XeroApiType.Product,
            ComponentUuid: "abcdef123",
            Description: "test-description",
            EnvironmentUrls: new Dictionary<string, string>
            {
                ["Uat"] = "invalid-url",
                ["Production"] = "https://test.prod.com"
            }
        );
        var stubDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new ApiInfoDocumentFilter(stubCatalogueMetadata, stubDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument { Info = new OpenApiInfo() };
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubDocumentFilterContext);

        // Assert
        stubDocCtx.ShouldWarn(
            "OpenAPI spec {0} Url is not valid. Please configure correct URLs for `CatalogueMetadata` in `Program.cs`.", "Uat");
    }
}
