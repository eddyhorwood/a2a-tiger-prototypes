// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Collections;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xero.Accelerators.Api.Core.Security.XeroAuthorisation;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.XeroAuthorisation;

public class ContextProviderTests
{
    [Fact]
    public void GivenGetUserId_WhenXeroUserIdProvidedInHttpContextFeature_ThenReturnsUserId()
    {
        // Arrange
        var stubUserId = Guid.NewGuid();
        var stubHttpContext = new DefaultHttpContext
        {
            Features = { [typeof(XeroUserIdFeature)] = new XeroUserIdFeature { XeroUserId = stubUserId } },
        };
        var sut = new ContextProvider(Mock.Of<IHttpContextAccessor>());

        // Act
        var actual = sut.GetUserId(stubHttpContext);

        // Assert
        actual.Id.Should().Be(stubUserId);
    }

    [Fact]
    public void
        GivenGetUserId_WhenXeroUserIdNotProvidedInHttpContextFeature_ThenThrowsExceptionWithXeroUserIdNotProvidedMessage()
    {
        // Arrange
        var stubHttpContext = new DefaultHttpContext();
        var sut = new ContextProvider(Mock.Of<IHttpContextAccessor>());

        // Act
        var act = () => sut.GetUserId(stubHttpContext);

        // Assert
        var exception = Assert.Throws<BadHttpRequestException>(act);

        exception.Message.Should().BeEquivalentTo("A Xero User ID value was not specified.");
        exception.Data.Contains(new DictionaryEntry("ProblemDetails.Identifier", "invalid-xero-user-id"));
        exception.Data.Contains(new DictionaryEntry("ProblemDetails.Title", "A Xero User ID value was not specified."));
    }

    [Fact]
    public void GetTenantId_WhenXeroTenantIdProvidedInRoute_ThenReturnsTenantId()
    {
        // Arrange
        var stubTenantId = Guid.NewGuid();
        var stubHttpContext = new DefaultHttpContext
        {
            Request =
            {
                RouteValues =
                {
                    ["xeroTenantId"] = stubTenantId.ToString()
                }
            }
        };
        var sut = new ContextProvider(Mock.Of<IHttpContextAccessor>());

        // Act
        var actual = sut.GetTenantId(stubHttpContext);

        // Assert
        actual.Id.Should().Be(stubTenantId);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("0000000-0000-0000-0000-000000001212")]
    public void
        GivenGetTenantId_WhenMalformedXeroTenantIdProvidedInRoute_ThenThrowsExceptionWithInvalidTenantIdMessage(
            string invalidUuid)
    {
        // Arrange
        var stubHttpContext = new DefaultHttpContext
        {
            Request = { RouteValues = { ["xeroTenantId"] = invalidUuid } }
        };
        var sut = new ContextProvider(Mock.Of<IHttpContextAccessor>());

        // Act
        var act = () => sut.GetTenantId(stubHttpContext);

        // Assert
        var exception = Assert.Throws<BadHttpRequestException>(act);

        exception.Message.Should().BeEquivalentTo("The Xero Tenant Id URL parameter value was not a valid GUID.");
        exception.Data.Contains(new DictionaryEntry("ProblemDetails.Identifier", "invalid-xero-tenant-id"));
        exception.Data.Contains(new DictionaryEntry("ProblemDetails.Title", "The Xero Tenant Id URL parameter value was not a valid GUID."));
    }
}
