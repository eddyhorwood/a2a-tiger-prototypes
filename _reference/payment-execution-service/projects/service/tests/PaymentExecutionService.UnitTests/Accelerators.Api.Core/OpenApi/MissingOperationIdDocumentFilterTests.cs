// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class MissingOperationIdDocumentFilterTests
{
    [Fact]
    public void MissingOperationIdDocumentFilter_AddErrorsIntoDocumentationContext_WhenOperationDoesNotHaveId()
    {
        // Arrange
        var mockDocCtx = new Mock<IOpenApiDocumentationContext>();
        var sut = new MissingOperationIdDocumentFilter(mockDocCtx.Object);

        var stubSwaggerDoc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/test-route"] = new()
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Get] = new() { OperationId = "test-id" },
                        [OperationType.Delete] = new(),
                        [OperationType.Post] = new(),
                    }
                }
            }
        };
        var stubFilterCtx = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(stubSwaggerDoc, stubFilterCtx);

        // Assert
        mockDocCtx.ShouldWarn("{0} operation at route '{1}' is missing an operation ID.", "DELETE", "/test-route");
        mockDocCtx.ShouldWarn("{0} operation at route '{1}' is missing an operation ID.", "POST", "/test-route");
    }
}
