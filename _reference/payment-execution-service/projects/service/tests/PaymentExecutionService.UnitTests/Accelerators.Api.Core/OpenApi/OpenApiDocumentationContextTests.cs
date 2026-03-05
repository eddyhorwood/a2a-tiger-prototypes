// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xero.Accelerators.Api.Core.OpenApi;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class OpenApiDocumentationContextTests
{
    [Fact]
    public void OpenApiDocumentationContext_LogsWarnings()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OpenApiDocumentationContext>>();
        var sut = new OpenApiDocumentationContext(mockLogger.Object);
        var stubWarning = "test warning {0} {1}";

        // Act
        sut.Warn(stubWarning, 777, "xyz");

        // Assert
        mockLogger.Verify(mock => mock.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((val, type) => val.ToString() == "OpenAPI warning: test warning 777 xyz"),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }
}
