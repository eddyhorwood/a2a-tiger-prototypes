// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;
using Xero.Accelerators.Api.Core.Security;
using Xero.Accelerators.Api.UnitTests.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security;

public class EndpointSecurityProviderTests
{
    [Fact]
    public async Task GivenGetAuthorisationAsync_WhenEndpointDoesNotContainAuthorizeDataMetadata_ThenReturnsEndpointAuthorisationNone()
    {
        // Arrange
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("test-endpoint");
        var sut = new EndpointSecurityProvider(
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IAuthorizationPolicyProvider>());

        // Act
        var endpointAuthorisation = await sut.GetEndpointSecurityAsync(endpoint);

        // Assert
        endpointAuthorisation.AuthenticationSchemes.Should().BeEmpty();
        endpointAuthorisation.AuthorisationRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuthorisationAsync_WhenEndpointContainsAuthorizeAttribute_And_AllowAnonymousMetadata_ThenReturnsEndpointAuthorisationNone()
    {
        // Arrange
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("test-endpoint", new AllowAnonymousAttribute(), new AuthorizeAttribute());
        var sut = new EndpointSecurityProvider(
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IAuthorizationPolicyProvider>());

        // Act
        var endpointAuthorisation = await sut.GetEndpointSecurityAsync(endpoint);

        // Assert
        endpointAuthorisation.AuthenticationSchemes.Should().BeEmpty();
        endpointAuthorisation.AuthorisationRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenGetAuthorisationAsync_WhenEndpointContainsNonExistentPolicy_ThenReturnsEndpointAuthorisationNone()
    {
        // Arrange
        var authAttribute = new AuthorizeAttribute { Policy = null };
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("test-endpoint", authAttribute);
        Mock<IAuthorizationPolicyProvider> stubAuthorizationPolicyProvider = new();
        stubAuthorizationPolicyProvider.Setup(x => x.GetPolicyAsync(authAttribute.Policy!))
            .ReturnsAsync((AuthorizationPolicy?)null);
        var sut = new EndpointSecurityProvider(
            Mock.Of<IAuthenticationSchemeProvider>(),
            stubAuthorizationPolicyProvider.Object);

        // Act
        var endpointAuthorisation = await sut.GetEndpointSecurityAsync(endpoint);

        // Assert
        endpointAuthorisation.AuthenticationSchemes.Should().BeEmpty();
        endpointAuthorisation.AuthorisationRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenGetAuthorisationAsync_WhenEndpointContainsAuthZMetadata_And_EmptyPolicy_ThenReturnsEndpointAuthorisationWithDefaultAuthScheme()
    {
        // Arrange
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("test-endpoint", new AuthorizeAttribute());
        Mock<IAuthorizationPolicyProvider> stubAuthorizationPolicyProvider = new();
        stubAuthorizationPolicyProvider.Setup(x => x.GetDefaultPolicyAsync())
            .ReturnsAsync(
                new AuthorizationPolicy(
                    new List<IAuthorizationRequirement> { new ClaimsAuthorizationRequirement("scope", new[] { "test-scope" }) },
                    new List<string> { "TestAuthenticationScheme" }
                )
            );
        var sut = new EndpointSecurityProvider(
            Mock.Of<IAuthenticationSchemeProvider>(),
            stubAuthorizationPolicyProvider.Object);
        var expectedEndpointAuthorisation = new EndpointSecurity { AuthorisationRequirements = { new ClaimsAuthorizationRequirement("scope", new[] { "test-scope" }) }, AuthenticationSchemes = { "TestAuthenticationScheme" } };

        // Act
        var endpointAuthorisation = await sut.GetEndpointSecurityAsync(endpoint);

        // Assert
        endpointAuthorisation.Should().BeEquivalentTo(expectedEndpointAuthorisation);
    }

    [Fact]
    public async Task GivenGetAuthorisationAsync_WhenEndpointContainsAuthZMetadata_And_ExistentPolicy_ThenReturnsEndpointAuthorisationWithAuthSchemeAndRequirement()
    {
        // Arrange
        var authorizeData = Mock.Of<IAuthorizeData>(x => x.Policy == "TestAuthorisationPolicy");
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint("test-endpoint", new AuthorizeAttribute(), authorizeData);
        Mock<IAuthorizationPolicyProvider> stubAuthorizationPolicyProvider = new();
        stubAuthorizationPolicyProvider.Setup(x => x.GetPolicyAsync("TestAuthorisationPolicy"))
            .ReturnsAsync(
                new AuthorizationPolicy(
                    new List<IAuthorizationRequirement> { Mock.Of<IAuthorizationRequirement>() },
                    new List<string> { "TestAuthenticationScheme" }
                )
            );
        var sut = new EndpointSecurityProvider(
            Mock.Of<IAuthenticationSchemeProvider>(),
            stubAuthorizationPolicyProvider.Object);
        var expectedEndpointAuthorisation = new EndpointSecurity { AuthorisationRequirements = { Mock.Of<IAuthorizationRequirement>() }, AuthenticationSchemes = { "TestAuthenticationScheme" } };

        // Act
        var endpointAuthorisation = await sut.GetEndpointSecurityAsync(endpoint);

        // Assert
        endpointAuthorisation.Should().BeEquivalentTo(expectedEndpointAuthorisation);
    }
}
