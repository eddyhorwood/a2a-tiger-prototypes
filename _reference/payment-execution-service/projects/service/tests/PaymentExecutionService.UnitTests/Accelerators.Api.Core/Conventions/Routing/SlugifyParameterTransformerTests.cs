// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics;
using FluentAssertions;
using Xero.Accelerators.Api.Core.Conventions.Routing;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Conventions.Routing;

public class SlugifyParameterTransformerTests
{
    public SlugifyParameterTransformerTests()
    {
        Trace.Listeners.Clear(); //prevent debug asserts from firing
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenSlugifyParameterTransformer_WhenRouteIsAnEmptyOrWhitespaceString_ThenReturnsNull(string route)
    {
        // Arrange
        var test = new SlugifyParameterTransformer();

        // Act
        var output = test.TransformOutbound(route);

        // Assert
        output.Should().BeNull();
    }

    [Theory]
    [InlineData("Test", "test")]
    [InlineData("test", "test")]
    [InlineData("TwoWORDS", "two-words")]
    [InlineData("Naveen-wasHere", "naveen-was-here")]
    public void GivenSlugifyParameterTransformer_WhenRouteIsTransformed_ThenReturnsSlugifiedRoute(string route, string expectedOutput)
    {
        // Arrange
        var test = new SlugifyParameterTransformer();

        // Act
        var output = test.TransformOutbound(route);

        // Assert
        output.Should().Be(expectedOutput);
    }
}
