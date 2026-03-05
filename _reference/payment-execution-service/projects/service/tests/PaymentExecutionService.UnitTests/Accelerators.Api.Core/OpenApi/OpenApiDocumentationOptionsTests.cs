// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Linq;
using FluentAssertions;
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xero.Accelerators.Api.Core.Observability.HealthChecks;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class OpenApiDocumentationOptionsTests
{
    [Fact]
    public void OpenApiDocumentationOptions_OrdersDocumentFilters()
    {
        // Arrange
        var sut = new OpenApiDocumentationOptions();
        sut.AddFilter<ApiInfoDocumentFilter>(2000);
        sut.AddFilter<MissingOperationIdDocumentFilter>(500);
        sut.AddFilter<HealthChecksDocumentFilter>(500);
        sut.AddFilter<XeroTenantRouteDocumentFilter>(1000);

        // Act
        var actual = sut.DocumentFilters.ToList();

        // Assert
        actual[0].Type.Should().Be(typeof(HealthChecksDocumentFilter));
        actual[1].Type.Should().Be(typeof(MissingOperationIdDocumentFilter));
        actual[2].Type.Should().Be(typeof(XeroTenantRouteDocumentFilter));
        actual[3].Type.Should().Be(typeof(ApiInfoDocumentFilter));
    }
}
