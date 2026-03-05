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

public class ProviderAccountIdHeaderDocumentFilterTests
{
    [Fact]
    public void
        GivenEndpointHasRequiredProviderAccountIdHeaderAttribute_WhenOpenApiSpecGenerated_ThenIncludesProviderAccountIdInSpec()
    {
        var builder = new OpenApiTestsDocBuilder();
        var endpointWithoutRequiredAttribute = builder.AddEndpoint();
        var endpointWithRequiredAttribute = builder.AddEndpoint(new RequiresProviderAccountIdHeaderAttribute());

        var stubContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());
        var expectedParameterOpenApiSpec = new OpenApiParameter
        {
            Name = Constants.HttpHeaders.ProviderAccountId,
            In = ParameterLocation.Header,
            Description = "Provider Account Id",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        };

        // Act
        var sut = new ProviderAccountIdHeaderDocumentFilter(builder.Endpoints);
        sut.Apply(builder.Document, stubContext);

        // Assert
        endpointWithRequiredAttribute.Parameters.Should().ContainEquivalentOf(expectedParameterOpenApiSpec);
        endpointWithoutRequiredAttribute.Parameters.Should().BeEmpty();
    }
}
