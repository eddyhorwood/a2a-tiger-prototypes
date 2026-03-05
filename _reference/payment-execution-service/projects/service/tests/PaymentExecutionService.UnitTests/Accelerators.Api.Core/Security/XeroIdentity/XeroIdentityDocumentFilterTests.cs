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
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Security;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.XeroIdentity;

public class XeroIdentityDocumentFilterTests
{
    private readonly OpenApiSecurityScheme _xeroIdentitySecurityScheme = new()
    {
        Reference = new OpenApiReference { Id = "XeroIdentity", Type = ReferenceType.SecurityScheme },
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                RefreshUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute),
                TokenUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute)
            }
        }
    };

    [Fact]
    public void
        GivenXeroIdentityDocumentFilter_WhenEndpointIsSecuredByIdentity_ThenScopesUsedByEndpointsAreAddedToSecuritySchemes()
    {
        // Arrange
        AssertionOptions.FormattingOptions.MaxDepth = 6;
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthorisationRequirements =
                {
                    new ClaimsAuthorizationRequirement("scope",
                        new[] { "scope-1", "scope-2" })
                },
                AuthenticationSchemes = { "Bearer" }
            });
        var sut = new XeroIdentityDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var expectedXeroIdentitySecurityScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Id = "XeroIdentity", Type = ReferenceType.SecurityScheme },
            Type = SecuritySchemeType.OAuth2,
            Description = "Xero Identity Security Scheme",
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute),
                    RefreshUrl = new Uri("https://identity.xero.com/connect/token", UriKind.Absolute),
                    Scopes = new Dictionary<string, string> { { "scope-1", "scope-1" } }
                }
            }
        };
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Security.Should().ContainEquivalentOf(new OpenApiSecurityRequirement
        {
            { expectedXeroIdentitySecurityScheme, new[] { "scope-1", "scope-2" } }
        });
    }

    [Fact]
    public void GivenXeroIdentityDocumentFilter_WhenEndpointHasAuthentication_ThenAddResponseWithSecurityRequirement()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthorisationRequirements =
                {
                    new ClaimsAuthorizationRequirement("scope",
                        new[] { "scope-1", "scope-2" })
                },
                AuthenticationSchemes = { "Bearer" }
            });
        var sut = new XeroIdentityDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Security.Should().BeEquivalentTo(new[]
        {
            new OpenApiSecurityRequirement { { _xeroIdentitySecurityScheme, new[] { "scope-1", "scope-2" } } }
        });
    }

    [Fact]
    public void GivenXeroIdentityDocumentFilter_WhenEndpointDoesNotHaveAuthentication_ThenDoesNotAddASecurityScheme()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(new EndpointSecurity());
        var sut = new XeroIdentityDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Security.Should().BeEmpty();
    }
}
