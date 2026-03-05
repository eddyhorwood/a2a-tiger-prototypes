// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using FluentAssertions;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Security.XeroIdentity;

public class IdentityConfigurationTests
{
    [Fact]
    public void RequireHttpsMetadata_WhenIdentityUsesHttpsAuthority_ReturnsTrue()
    {
        // Arrange
        var identityConfig = new IdentityOptions
        {
            Authority = "https://identity.example.com",
        };

        // Assert
        identityConfig.RequireHttpsMetadata.Should().Be(true);
    }

    [Fact]
    public void RequireHttpsMetadata_WhenIdentityUsesHttpAuthority_ReturnsFalse()
    {
        // Arrange
        var identityConfig = new IdentityOptions
        {
            Authority = "http://identity.example.com",
        };

        // Assert
        identityConfig.RequireHttpsMetadata.Should().Be(false);

    }

    [Fact]
    public void RequireHttpsMetadata_WhenIdentityUsesInvalidAuthority_ReturnsFalse()
    {
        // Arrange
        var identityConfig = new IdentityOptions
        {
            Authority = "invalid-url",
        };

        // Assert
        identityConfig.RequireHttpsMetadata.Should().Be(false);
    }
}
