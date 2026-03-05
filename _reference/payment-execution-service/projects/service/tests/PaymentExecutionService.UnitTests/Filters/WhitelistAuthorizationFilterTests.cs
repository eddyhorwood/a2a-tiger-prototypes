using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentExecution.TestUtilities;
using PaymentExecutionService.Filters;
using PaymentExecutionService.Models;
using Xunit;
using static PaymentExecutionService.UnitTests.TestUtilities;

namespace PaymentExecutionService.UnitTests.Filters;

public class WhitelistAuthorizationFilterTests
{
    private readonly Mock<IOptions<WhitelistOptions>> _mockOptions;
    private readonly Mock<ILogger<WhitelistAuthorizationFilter>> _mockLogger;
    private readonly WhitelistAuthorizationFilter _sut;
    private const string ExpectedLogPrefix = "Client ID '{ClientId}' attempting to access the service is not whitelisted.";
    private readonly WhitelistOptions _baseOptionsConfiguration = new WhitelistOptions
    {
        ClientIds = ["whitelisted-Client-Id"]
    };

    public WhitelistAuthorizationFilterTests()
    {
        _mockOptions = new Mock<IOptions<WhitelistOptions>>();
        _mockLogger = new Mock<ILogger<WhitelistAuthorizationFilter>>();
        _sut = new WhitelistAuthorizationFilter(_mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public void GivenWhitelistOptionsIsNull_WhenWhitelistAuthorizationFilterInvoked_ThenLogAndThrowArgumentNullException()
    {
        // Arrange
        _mockOptions.Setup(opts => opts.Value).Returns((WhitelistOptions)null!);
        var expectedLogPrefix = "Whitelist has not been configured correctly";
        var mockedClaims = new List<Claim>
        {
            new ("client_id", "whitelisted-Client-Id"),
        };
        var bearerTokenString = GenerateBearerTokenWithClaims(mockedClaims);
        var httpContext = CreateHttpContextWithAuthorizationJwtToken(bearerTokenString);
        var authContext = CreateAuthContextFromHttpContext(httpContext);

        // Act
        var act = () => _sut.OnAuthorization(authContext);

        // Assert
        Assert.Throws<NoNullAllowedException>(act);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Error, expectedLogPrefix, 1);
    }

    [Fact]
    public void GivenClientIdNotPresentInToken_WhenWhitelistAuthorizationFilterInvoked_ThenLogsAndSetsForbiddenResult()
    {
        // Arrange
        var expectedClientId = string.Empty;
        var expectedLogPrefix = ExpectedLogPrefix.Replace("{ClientId}", expectedClientId);

        _mockOptions.Setup(o => o.Value).Returns(_baseOptionsConfiguration);
        var bearerTokenStringWithNoClaims = GenerateBearerTokenWithClaims(null);
        var httpContext = CreateHttpContextWithAuthorizationJwtToken(bearerTokenStringWithNoClaims);
        var authContext = CreateAuthContextFromHttpContext(httpContext);

        // Act
        _sut.OnAuthorization(authContext);

        // Assert
        var responseObjectResult = (ObjectResult)authContext.Result!;
        Assert.Equal(StatusCodes.Status403Forbidden, responseObjectResult.StatusCode);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Warning, expectedLogPrefix, 1);
    }

    [Fact]
    public void GivenClientIdIsPresentButIsNotInPermittedList_WhenWhitelistAuthorizationFilterInvoked_ThenLogsAndSetsForbiddenResult()
    {
        // Arrange
        var mockClientId = "some-client-id";
        var expectedLogPrefix = ExpectedLogPrefix.Replace("{ClientId}", mockClientId);

        _mockOptions.Setup(o => o.Value).Returns(_baseOptionsConfiguration);
        var mockedClaims = new List<Claim>
        {
            new ("client_id", mockClientId),
        };
        var bearerTokenString = GenerateBearerTokenWithClaims(mockedClaims);
        var httpContext = CreateHttpContextWithAuthorizationJwtToken(bearerTokenString);
        var authContext = CreateAuthContextFromHttpContext(httpContext);

        // Act
        _sut.OnAuthorization(authContext);

        // Assert
        var responseObjectResult = (ObjectResult)authContext.Result!;
        Assert.Equal(StatusCodes.Status403Forbidden, responseObjectResult.StatusCode);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Warning, expectedLogPrefix, 1);
    }

    [Fact]
    public void GivenClientIdIsPresentAndWhitelisted_WhenWhitelistAuthorizationFilterInvoked_ThenNoContextResultIsSet()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(_baseOptionsConfiguration);
        var mockedClaims = new List<Claim>
        {
            new ("client_id", "whitelisted-Client-Id"),
        };
        var bearerTokenString = GenerateBearerTokenWithClaims(mockedClaims);
        var httpContext = CreateHttpContextWithAuthorizationJwtToken(bearerTokenString);
        var authContext = CreateAuthContextFromHttpContext(httpContext);

        // Act
        _sut.OnAuthorization(authContext);

        // Assert that no object result is set
        Assert.Null(authContext.Result);
    }

    private static DefaultHttpContext CreateHttpContextWithAuthorizationJwtToken(string jwtToken)
    {
        var mockOptionsService = new Mock<IProblemDetailsService>();
        var context = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IProblemDetailsService>(mockOptionsService.Object)
                .BuildServiceProvider()
        };
        context.Request.Headers.Authorization = jwtToken;

        return context;
    }

    private static AuthorizationFilterContext CreateAuthContextFromHttpContext(DefaultHttpContext httpContext)
    {
        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            new List<IFilterMetadata>());
    }
}

