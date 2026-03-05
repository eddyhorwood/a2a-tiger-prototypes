// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Xero.Accelerators.Api.Core.OpenApi;
using Xero.Accelerators.Api.Core.Security;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public static class OpenApiTestsHelpers
{
    public static void ShouldWarn(
        this Mock<IOpenApiDocumentationContext> mock, string message, params object[] args)
    {
        mock.Verify(oadc => oadc.Warn(message, args), Times.Once());
    }

    public static RouteEndpoint CreateRouteEndpoint(string endpointName, params object[] metadata)
    {
        var builder = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse($"/{endpointName}"),
            order: 0
        );

        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            builder.Metadata.Add(new EndpointNameMetadata(endpointName));
        }

        foreach (var metadataItem in metadata)
        {
            builder.Metadata.Add(metadataItem);
        }

        return (RouteEndpoint)builder.Build();
    }

    public static IEndpointSecurityProvider CreateStubEndpointSecurityProvider(EndpointSecurity endpointSecurity)
    {
        var stubEndpointSecurityProvider = new Mock<IEndpointSecurityProvider>();
        stubEndpointSecurityProvider.Setup(esp => esp.GetEndpointSecurityAsync(It.IsAny<RouteEndpoint>())).ReturnsAsync(endpointSecurity);

        return stubEndpointSecurityProvider.Object;
    }
}
