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
using Xero.Accelerators.Api.Core.Observability.Correlation;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;
using static Xero.Accelerators.Api.Core.Constants;

namespace Xero.Accelerators.Api.UnitTests.Observability.Correlation;

public class CorrelationIdDocumentFilterTests
{
    [Fact]
    public void CorrelationIdDocumentFilter_AddXeroCorrelationIdParameter_ForOperationsThatRequireXeroCorrelationId()
    {
        // Arrange
        var builder = new OpenApiTestsDocBuilder();
        var endpointWithoutMetadata = builder.AddEndpoint();
        var endpointWithMetadata = builder.AddEndpoint(new AllowNoXeroCorrelationIdAttribute());
        var sut = new CorrelationIdDocumentFilter(builder.Endpoints);
        var stubContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubContext);

        // Assert
        endpointWithoutMetadata.Parameters.Should().ContainEquivalentOf(new OpenApiParameter
        {
            Name = HttpHeaders.XeroCorrelationId,
            In = ParameterLocation.Header,
            Description = "Xero Correlation Id",
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
        endpointWithMetadata.Parameters.Should().NotContain(p => p.Name == HttpHeaders.XeroCorrelationId);
    }
}
