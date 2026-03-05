// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xero.Accelerators.Api.Core;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.XeroIdentity;

public class XeroUserIdMiddlewareTests
{
    [Theory]
    [InlineData(XeroUserIdSource.Header)]
    [InlineData(XeroUserIdSource.Header | XeroUserIdSource.Claims)]
    public async Task GivenXeroUserIdMiddleware_WhenXeroUserIdSourceIncludesHeaderAndTheHeaderExists_ThenPopulatesXeroUserIdFeature(XeroUserIdSource source)
    {
        // Arrange
        var stubXeroUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext { Request = { Headers = { ["Xero-User-Id"] = stubXeroUserId.ToString() } } };

        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = source };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroUserIdMiddleware(Mock.Of<RequestDelegate>(), stubOptionMonitors);

        // Act
        await sut.InvokeAsync(httpContext);

        // Assert
        var xeroUserIdFeature = httpContext.Features.Get<XeroUserIdFeature>();
        xeroUserIdFeature.Should().NotBeNull();
        xeroUserIdFeature!.XeroUserId.Should().Be(stubXeroUserId);
    }

    [Fact]
    public async Task GivenXeroUserIdMiddleware_WhenXeroUserIdSourceIsHeaderAndTheHeaderDoesNotExist_ThenDoesNotPopulateXeroUserIdFeature()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = XeroUserIdSource.Header };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroUserIdMiddleware(Mock.Of<RequestDelegate>(), stubOptionMonitors);

        // Act
        await sut.InvokeAsync(httpContext);

        // Assert
        var xeroUserIdFeature = httpContext.Features.Get<XeroUserIdFeature>();
        xeroUserIdFeature.Should().BeNull();
    }

    [Theory]
    [InlineData(XeroUserIdSource.Claims)]
    [InlineData(XeroUserIdSource.Header | XeroUserIdSource.Claims)]
    public async Task GivenXeroUserIdMiddleware_WhenXeroUserIdSourceIncludesClaimsAndTheClaimsExists_ThenPopulatesXeroUserIdFeature(XeroUserIdSource source)
    {
        // Arrange
        var stubXeroUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(Constants.ClaimTypes.XeroUserId, stubXeroUserId.ToString())
            }))
        };

        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = source };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroUserIdMiddleware(Mock.Of<RequestDelegate>(), stubOptionMonitors);

        // Act
        await sut.InvokeAsync(httpContext);

        // Assert
        var xeroUserIdFeature = httpContext.Features.Get<XeroUserIdFeature>();
        xeroUserIdFeature.Should().NotBeNull();
        xeroUserIdFeature!.XeroUserId.Should().Be(stubXeroUserId);
    }

    [Fact]
    public async Task GivenXeroUserIdMiddleware_WhenXeroUserIdSourceIsHeaderAndTheClaimDoesNotExist_ThenPopulatesXeroUserIdFeature()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identityOptions = new XeroIdentityOptions { RetrieveUserIdFrom = XeroUserIdSource.Header };
        var stubOptionMonitors = Mock.Of<IOptionsMonitor<XeroIdentityOptions>>(m => m.CurrentValue == identityOptions);
        var sut = new XeroUserIdMiddleware(Mock.Of<RequestDelegate>(), stubOptionMonitors);

        // Act
        await sut.InvokeAsync(httpContext);

        // Assert
        var xeroUserIdFeature = httpContext.Features.Get<XeroUserIdFeature>();
        xeroUserIdFeature.Should().BeNull();
    }
}
