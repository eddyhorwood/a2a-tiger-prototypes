using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using PaymentExecutionService.Middleware;
using Xunit;

namespace PaymentExecutionService.UnitTests.Middleware;

public class TenantIdRequiredHeaderMiddlewareTests
{
    private readonly TenantIdRequiredHeaderMiddleware _sut;
    private readonly Mock<RequestDelegate> _nextMiddleware;
    private readonly Mock<IProblemDetailsService> _problemDetailsService;

    public TenantIdRequiredHeaderMiddlewareTests()
    {
        _nextMiddleware = new Mock<RequestDelegate>();
        _sut = new TenantIdRequiredHeaderMiddleware(_nextMiddleware.Object);
        _problemDetailsService = new Mock<IProblemDetailsService>();
    }

    [Fact]
    public async Task
        GivenTenantIdHeaderRequiredEndpointAndValidValue_WhenTenantIdRequiredHeaderMiddlewareInvoked_ThenNoBadRequestOrProblemDetails()
    {
        var httpContext = new DefaultHttpContext();
        var mockTenantId = Guid.NewGuid();
        var tenantIdHeaderKvp = CreateTenantIdHeaderWithValue(mockTenantId.ToString());
        httpContext.Request.Headers.Add(tenantIdHeaderKvp);
        CreateEndpointWithRequiredAttribute(httpContext);

        await _sut.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        _problemDetailsService.Verify(m => m.WriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Never);
    }

    [Fact]
    public async Task
        GivenEndpointHasNoRequiredAttributeAndNoHeaderProvided_WhenTenantIdRequiredHeaderMiddlewareInvoked_ThenNoBadRequestOrProblemDetails()
    {
        var httpContext = new DefaultHttpContext();
        CreateEndpointWithoutRequiredAttribute(httpContext);

        var act = async () => await _sut.InvokeAsync(httpContext);

        var exceptions = await Record.ExceptionAsync(act);
        Assert.Null(exceptions);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        _problemDetailsService.Verify(m => m.WriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Never);
    }

    [Theory]
    [InlineData("non-guid")]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task
        GivenEndpointRequiresTenantIdHeaderAndHeaderIsInvalidValue_WhenTenantIdRequiredHeaderMiddlewareInvoked_ThenReturnsBadRequest(string? invalidHeaderValue)
    {
        var httpContext = new DefaultHttpContext();
        var tenantIdHeaderKvp = CreateTenantIdHeaderWithValue(invalidHeaderValue);
        httpContext.Request.Headers.Add(tenantIdHeaderKvp);
        CreateEndpointWithRequiredAttribute(httpContext);

        await _sut.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        _problemDetailsService.Verify(m => m.WriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Once);
    }

    private static KeyValuePair<string, StringValues> CreateTenantIdHeaderWithValue(string? headerValue)
    {
        return new KeyValuePair<string, StringValues>(Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId, headerValue);
    }

    private void CreateEndpointWithoutRequiredAttribute(HttpContext httpContext)
    {
        var mockEndpoint = new Endpoint(_nextMiddleware.Object, null, "testEndpoint");
        var endpointFeature = new EndpointFeature(mockEndpoint);
        httpContext.Features.Set<IEndpointFeature>(endpointFeature);
    }

    private void CreateEndpointWithRequiredAttribute(HttpContext httpContext)
    {
        var requiredAttribute = new RequiresTenantIdHeaderAttribute();
        var mockMetaData = new EndpointMetadataCollection(requiredAttribute);
        var mockEndpoint = new Endpoint(_nextMiddleware.Object, mockMetaData, "testEndpoint");
        var endpointFeature = new EndpointFeature(mockEndpoint);
        httpContext.Features.Set<IEndpointFeature>(endpointFeature);

        var mockServiceCollection = new ServiceCollection();
        mockServiceCollection.AddSingleton(_problemDetailsService.Object);
        var mockServiceProvider = mockServiceCollection.BuildServiceProvider();
        httpContext.RequestServices = mockServiceProvider;
    }

    private class EndpointFeature : IEndpointFeature
    {
        public EndpointFeature(Endpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public Endpoint? Endpoint { get; set; }
    }
}
