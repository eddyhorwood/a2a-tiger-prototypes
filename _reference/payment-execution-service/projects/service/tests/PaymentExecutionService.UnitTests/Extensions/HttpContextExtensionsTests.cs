using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PaymentExecutionService.Extensions;
using Xunit;
using static PaymentExecutionService.UnitTests.TestUtilities;

namespace PaymentExecutionService.UnitTests.Extensions;

public class HttpContextExtensionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GivenAuthHeaderNullOrEmpty_WhenGetClientIdFromJwtToken_ReturnsEmptyString(string? authHeader)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = authHeader;

        // Act
        var result = context.GetClientIdFromJwtToken();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenAuthHeaderDoesNotStartWithBearer_WhenGetClientIdFromJwtToken_ThenReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic abc123";

        // Act
        var result = context.GetClientIdFromJwtToken();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenAuthHeaderValidJwtWithoutClientIdClaim_WhenGetClientIdFromJwtToken_ThenReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var bearerTokenStringWithNoClaims = GenerateBearerTokenWithClaims(null);
        context.Request.Headers.Authorization = bearerTokenStringWithNoClaims;

        // Act
        var result = context.GetClientIdFromJwtToken();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenAuthHeaderValidJwtWithClientId_WhenGetClientIdFromJwtToken_ThenReturnsExpectedClientId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedClientId = "some-client-id";
        var mockedClaims = new List<Claim>
        {
            new ("client_id", expectedClientId),
        };
        var bearerTokenString = GenerateBearerTokenWithClaims(mockedClaims);
        context.Request.Headers.Authorization = bearerTokenString;

        // Act
        var result = context.GetClientIdFromJwtToken();

        // Assert
        Assert.Equal(expectedClientId, result);
    }

    [Fact]
    public void GivenAuthHeaderInvalidValidJwt_WhenGetClientIdFromJwtToken_ThenReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization
            = "Bearer invalid-token-format";

        // Act
        var result = context.GetClientIdFromJwtToken();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
