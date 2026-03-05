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
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Security;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.XeroAuthorisation;

public class AuthorisationDocumentFilterTests
{
    [Fact]
    public void Apply_WhenEndpointHasNoAuthnSchemeOrAuthzPolicy_ThenDoesNotAddResponse()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider =
            OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(new EndpointSecurity());
        var sut = new AuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Responses.Should().BeEquivalentTo(new OpenApiResponses());
    }

    [Fact]
    public void Apply_WhenEndpointHasAuthnSchemeButNoAuthZPolicy_ThenAddsUnauthorizedResponse()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthenticationSchemes = { "TestAuthenticationScheme" }
            });
        var sut = new AuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Responses.Should().BeEquivalentTo(new OpenApiResponses
        {
            ["401"] = new() { Description = "Unauthorized" }
        });
    }

    [Fact]
    public void
        Apply_WhenEndpointHasAuthnSchemeAndAuthZPolicy_ThenAddsUnauthorizedAndForbiddenResponses()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthorisationRequirements = { new ClaimsAuthorizationRequirement("scope", new[] { "test-scope" }) },
                AuthenticationSchemes = { "TestAuthenticationScheme" }
            });
        var sut = new AuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Responses.Should().BeEquivalentTo(new OpenApiResponses
        {
            ["401"] = new() { Description = "Unauthorized" },
            ["403"] = new() { Description = "Forbidden" }
        });
    }
}
