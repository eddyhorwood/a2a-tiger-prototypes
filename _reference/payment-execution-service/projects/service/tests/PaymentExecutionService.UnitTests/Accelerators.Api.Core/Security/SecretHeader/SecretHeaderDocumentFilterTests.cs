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
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Security;
using Xero.Accelerators.Api.Core.Security.SecretHeader;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.SecretHeader;

public class SecretHeaderDocumentFilterTests
{
    private static readonly IOptions<SecretHeaderOptions> _secretHeaderOptions = Options.Create(new SecretHeaderOptions { SecretHeaderName = "test-key" });

    private static readonly OpenApiSecurityScheme _secretHeaderSecurityScheme = new()
    {
        Reference = new OpenApiReference { Id = "SecretHeader", Type = ReferenceType.SecurityScheme },
        Type = SecuritySchemeType.ApiKey,
        Name = "test-key",
        In = ParameterLocation.Header
    };

    [Fact]
    public void GivenSecretHeaderDocumentFilter_WhenEndpointIsSecuredBySecretHeaderRequired_ThenAddsSecretHeaderSecurityScheme()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthenticationSchemes = { "TestSecretHeaderAuthentication" },
                AuthorisationRequirements = { Mock.Of<DenyAnonymousAuthorizationRequirement>() }
            });
        var sut = new SecretHeaderDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, _secretHeaderOptions, "TestSecretHeaderAuthentication");
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Security.Should().ContainEquivalentOf(new OpenApiSecurityRequirement
        {
            { _secretHeaderSecurityScheme, Array.Empty<string>() }
        });
    }

    [Fact]
    public void GivenSecretHeaderDocumentFilter_WhenEndpointIsSecuredBySecretHeaderOptional_ThenDoesNotAddASecurityScheme()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthenticationSchemes = { "TestSecretHeaderAuthentication" }
            });
        var sut = new SecretHeaderDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, _secretHeaderOptions, "TestSecretHeaderAuthentication");
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Security.Should().BeEmpty();
    }

    [Fact]
    public void GivenSecretHeaderDocumentFilter_WhenOperationsAuthenticatedBySecretHeaderRequired_ThenAddResponseWithRequiredHeaderAndSecurityRequirement()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(new EndpointSecurity
        {
            AuthenticationSchemes = { "TestSecretHeaderAuthentication" },
            AuthorisationRequirements = { Mock.Of<DenyAnonymousAuthorizationRequirement>() }
        });
        var sut = new SecretHeaderDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, _secretHeaderOptions, "TestSecretHeaderAuthentication");
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Should().ContainEquivalentOf(new OpenApiParameter
        {
            Name = "test-key",
            In = ParameterLocation.Header,
            Description = "A secret value known only by the service owners.",
            Schema = new OpenApiSchema { Type = "string" },
            Required = true
        });
        endpoint.Security.Should().ContainEquivalentOf(new OpenApiSecurityRequirement
        {
            { _secretHeaderSecurityScheme, Array.Empty<string>() }
        });
    }

    [Fact]
    public void GivenSecretHeaderDocumentFilter_WhenOperationsAuthenticatedBySecretHeaderOptional_ThenAddResponseWithSecurityRequirement()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(new EndpointSecurity
        {
            AuthenticationSchemes = { "TestSecretHeaderAuthentication" },
        });
        var sut = new SecretHeaderDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, _secretHeaderOptions, "TestSecretHeaderAuthentication");
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(), Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Should().ContainEquivalentOf(new OpenApiParameter
        {
            Name = "test-key",
            In = ParameterLocation.Header,
            Description = "A secret value known only by the service owners.",
            Schema = new OpenApiSchema { Type = "string" },
        });
        endpoint.Security.Should().BeEmpty();
    }
}
