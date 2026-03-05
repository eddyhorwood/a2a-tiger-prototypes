// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xero.Accelerators.Api.Core.Security;
using Xero.Accelerators.Api.Core.Security.XeroAuthorisation;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Authorisation.Integration.Common;
using Xunit;
using static Xero.Accelerators.Api.Core.Constants;
using AssertionRequirement = Xero.Authorisation.Integration.NetCore.Sdk.Authorize.AssertionRequirement;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class XeroAuthorisationDocumentFilterTests
{
    [Theory]
    [InlineData(XeroUserIdSource.Header, true)]
    [InlineData(XeroUserIdSource.Header | XeroUserIdSource.Claims, false)]
    public void GivenApply_WhenEndpointAuthorisedByAuthZAndXeroUserIdFromRequestHeader_ThenAddsXeroUserIdHeaderParameter(XeroUserIdSource source, bool isHeaderRequired)
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthorisationRequirements = { new AssertionRequirement(new Action(new AuthNamespace("test"), "")) }
            });
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = source };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroAuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, stubOptionMonitors);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Should().ContainEquivalentOf(new OpenApiParameter
        {
            Name = HttpHeaders.XeroUserId,
            In = ParameterLocation.Header,
            Description = "Xero User Id",
            Required = isHeaderRequired,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }

    [Fact]
    public void GivenApply_WhenEndpointAuthorisedByAuthZAndXeroUserIdNotFromRequestHeader_ThenDoesNotAddXeroUserIdHeaderParameter()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider = OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(
            new EndpointSecurity
            {
                AuthorisationRequirements = { new AssertionRequirement(new Action(new AuthNamespace("test"), "")) }
            });
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = XeroUserIdSource.Claims };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroAuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, stubOptionMonitors);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Should().NotContain(p => p.Name == HttpHeaders.XeroUserId);
    }

    [Fact]
    public void GivenApply_WhenNonAuthorisedEndpoint_ThenDoesNotAddXeroUserIdHeaderParameter()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var stubEndpointSecurityProvider =
            OpenApiTestsHelpers.CreateStubEndpointSecurityProvider(new EndpointSecurity());
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = XeroUserIdSource.Header };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroAuthorisationDocumentFilter(builder.Endpoints, stubEndpointSecurityProvider, stubOptionMonitors);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Should().NotContain(p => p.Name == HttpHeaders.XeroUserId);
    }

    [Fact]
    public void GivenApply_WhenXeroUserIdHeaderAlreadyExists_ThenDoesNotAddExtraHeader()
    {
        // Arrange
        OpenApiTestsDocBuilder builder = new();
        var endpoint = builder.AddEndpoint();
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = XeroUserIdSource.Header };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroAuthorisationDocumentFilter(builder.Endpoints, new Mock<IEndpointSecurityProvider>().Object, stubOptionMonitors);
        var existingUserIdParameter = new OpenApiParameter
        {
            Name = HttpHeaders.XeroUserId,
            In = ParameterLocation.Header,
            Description = "Existing Xero User Id",
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        endpoint.Parameters.Add(existingUserIdParameter);
        var stubDocumentFilterContext = new DocumentFilterContext(Mock.Of<IEnumerable<ApiDescription>>(),
            Mock.Of<ISchemaGenerator>(), new SchemaRepository());

        // Act
        sut.Apply(builder.Document, stubDocumentFilterContext);

        // Assert
        endpoint.Parameters.Count(p => p.Name == HttpHeaders.XeroUserId).Should().Be(1);
    }
}
