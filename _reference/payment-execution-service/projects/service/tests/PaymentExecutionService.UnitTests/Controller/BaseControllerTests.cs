using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentExecutionService.Controllers;
using Xunit;

namespace PaymentExecutionService.UnitTests.Controller;

public class BaseControllerTests
{
    private readonly TestBaseController _sut;

    public BaseControllerTests()
    {
        _sut = new TestBaseController();
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012")]
    [InlineData("DEADBEEF-DEAD-BEEF-DEAD-BEEFDEADBEEF")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
    public void GivenHeaderContainsValidGuid_WhenGettingXeroTenantId_ThenReturnsParsedGuid(string guidString)
    {
        // Arrange
        var expectedTenantId = Guid.Parse(guidString);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId] = guidString;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroTenantId;

        // Assert
        Assert.Equal(expectedTenantId, result);
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenHeaderContainsInvalidGuid_WhenGettingXeroTenantId_ThenReturnsEmptyGuid(string invalidGuid)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId] = invalidGuid;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroTenantId;

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void GivenHeaderIsNull_WhenGettingXeroTenantId_ThenReturnsEmptyGuid()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // Don't set the header at all
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroTenantId;

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012")]
    [InlineData("DEADBEEF-DEAD-BEEF-DEAD-BEEFDEADBEEF")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
    public void GivenHeaderContainsValidGuid_WhenGettingXeroCorrelationId_ThenReturnsParsedGuid(string guidString)
    {
        // Arrange
        var expectedCorrelationId = Guid.Parse(guidString);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroCorrelationId] = guidString;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroCorrelationId;

        // Assert
        Assert.Equal(expectedCorrelationId, result);
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenHeaderContainsInvalidGuid_WhenGettingXeroCorrelationId_ThenReturnsEmptyGuid(string invalidGuid)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroCorrelationId] = invalidGuid;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroCorrelationId;

        // Assert
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void GivenHeaderIsNull_WhenGettingXeroCorrelationId_ThenReturnsEmptyGuid()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // Don't set the header at all
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.XeroCorrelationId;

        // Assert
        Assert.Equal(Guid.Empty, result);
    }
}

// Test implementation of BaseController to access the properties
public class TestBaseController : BaseController
{
    // Expose the properties for testing
    public new Guid XeroTenantId => base.XeroTenantId;
    public new Guid XeroCorrelationId => base.XeroCorrelationId;
}
