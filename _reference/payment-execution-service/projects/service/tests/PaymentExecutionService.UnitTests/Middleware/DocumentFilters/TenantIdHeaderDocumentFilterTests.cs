using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using PaymentExecutionService.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware.DocumentFilters;

public class TenantIdHeaderDocumentFilterTests
{
    [Fact]
    public void
        GivenEndpointHasRequiredTenantIdHeaderAttribute_WhenOpenApiSpecGenerated_ThenIncludesXeroTenantIdInSpec()
    {
        var builder = new OpenApiTestsDocBuilder();
        var endpointWithoutRequiredAttribute = builder.AddEndpoint();
        var endpointWithRequiredAttribute = builder.AddEndpoint(new RequiresTenantIdHeaderAttribute());

        var stubContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());
        var expectedParameterOpenApiSpec = new OpenApiParameter
        {
            Name = Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId,
            In = ParameterLocation.Header,
            Description = "Xero Tenant Id",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        };

        // Act
        var sut = new TenantIdHeaderDocumentFilter(builder.Endpoints);
        sut.Apply(builder.Document, stubContext);

        // Assert
        endpointWithRequiredAttribute.Parameters.Should().ContainEquivalentOf(expectedParameterOpenApiSpec);
        endpointWithoutRequiredAttribute.Parameters.Should().BeEmpty();
    }
}
