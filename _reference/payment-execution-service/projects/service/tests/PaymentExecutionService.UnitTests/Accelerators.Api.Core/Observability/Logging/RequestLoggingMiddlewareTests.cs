// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xero.Accelerators.Api.Core.Observability.Logging;
using Xero.Accelerators.Api.Core.Observability.Monitoring;
using Xunit;

namespace Xero.Accelerators.Api.UnitTests.Observability.Logging;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestHasCorrelationId_SendsLogsWithCorrelationIdTag()
    {
        // Arrange
        var stubCorrelationId = Guid.NewGuid().ToString();
        var stubHttpContext = new DefaultHttpContext { Request = { Headers = { ["Xero-Correlation-Id"] = stubCorrelationId } } };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(l => l.BeginScope(It.Is<Dictionary<string, string>>(tags => tags.ContainsKey("Xero-Correlation-Id") && tags["Xero-Correlation-Id"].ToString() == stubCorrelationId)), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestHasNoCorrelationId_SendsLogsWithoutCorrelationIdTag()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(new DefaultHttpContext());

        // Assert
        mockLogger.Verify(l => l.BeginScope(It.Is<Dictionary<string, object>>(tags => tags.ContainsKey("Xero-Correlation-Id"))), Times.Never);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_ForAllRequests_SendsLogsWithNewRelicTraceIdTag()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var mockMonitoringService = new Mock<IMonitoringService>();
        var stubNewRelicTraceId = Guid.NewGuid().ToString();
        mockMonitoringService.Setup(agent => agent.GetTraceId()).Returns(stubNewRelicTraceId);
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, mockMonitoringService.Object);

        // Act
        await sut.Invoke(new DefaultHttpContext());

        // Assert
        mockLogger.Verify(l => l.BeginScope(It.Is<Dictionary<string, string>>(tags => tags.ContainsKey("NewRelicTraceId") && tags["NewRelicTraceId"].ToString() == stubNewRelicTraceId)), Times.Once);
    }

    [Theory]
    [InlineData(StatusCodes.Status102Processing, LogLevel.Information)]
    [InlineData(StatusCodes.Status200OK, LogLevel.Information)]
    [InlineData(StatusCodes.Status302Found, LogLevel.Information)]
    [InlineData(StatusCodes.Status400BadRequest, LogLevel.Information)]
    [InlineData(StatusCodes.Status501NotImplemented, LogLevel.Error)]
    public async Task RequestLoggingMiddleware_AccordingToHttpResponseStatusCode_LogsAtCorrectLevelWithMessageTemplate(int statusCode, LogLevel expectedLogLevel)
    {
        // Arrange
        var stubHttpContext = new DefaultHttpContext { Response = { StatusCode = statusCode }, Request = { Method = HttpMethods.Get, Path = "/test" } };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(mock => mock.Log(
            expectedLogLevel,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((val, type) => val.ToString() == $"HTTP {stubHttpContext.Request.Method} {stubHttpContext.Request.Path} responded {stubHttpContext.Response.StatusCode}"),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenHttpResponseInternalServerError_LogErrorsWithErrorTags()
    {
        // Arrange
        var stubHttpContext = new DefaultHttpContext { Response = { StatusCode = StatusCodes.Status501NotImplemented }, Request = { Method = HttpMethods.Get, Path = "/test" }, };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(mock => mock.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((val, type) => val.ToString() == $"HTTP {stubHttpContext.Request.Method} {stubHttpContext.Request.Path} responded {stubHttpContext.Response.StatusCode}"),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenExceptionThrown_LogErrorWithStatusCode500()
    {
        // Arrange
        var stubHttpContext = new DefaultHttpContext { Response = { StatusCode = StatusCodes.Status500InternalServerError }, Request = { Method = HttpMethods.Get, Path = "/test" }, };
        var requestDelegate = new RequestDelegate(_ => throw new Exception("test-exception"));
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(requestDelegate, mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.Invoke(stubHttpContext));
        mockLogger.Verify(mock => mock.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((val, type) => val.ToString() == $"HTTP {stubHttpContext.Request.Method} {stubHttpContext.Request.Path} responded {stubHttpContext.Response.StatusCode}"),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenExceptionThrown_NotifyNewRelicWithCorrelationId()
    {
        // Arrange
        var stubCorrelationId = Guid.NewGuid().ToString();
        var stubException = new Exception("test-exception");
        var requestDelegate = new RequestDelegate(_ => throw stubException);
        var mockMonitoringService = new Mock<IMonitoringService>();
        var sut = new RequestLoggingMiddleware(requestDelegate, Mock.Of<ILogger<RequestLoggingMiddleware>>(), mockMonitoringService.Object);
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Get,
                Path = "/test",
                Headers = { ["Xero-Correlation-Id"] = stubCorrelationId },
            },
        };
        var expectedAttributes = new Dictionary<string, string> { ["Xero-Correlation-Id"] = stubCorrelationId };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.Invoke(httpContext));
        mockMonitoringService.Verify(a => a.ReportError(stubException, expectedAttributes), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenExceptionThrown_NotifyNewRelic()
    {
        // Arrange
        var stubException = new Exception("test-exception");
        var requestDelegate = new RequestDelegate(_ => throw stubException);
        var mockMonitoringService = new Mock<IMonitoringService>();
        var sut = new RequestLoggingMiddleware(requestDelegate, Mock.Of<ILogger<RequestLoggingMiddleware>>(), mockMonitoringService.Object);
        var httpContext = new DefaultHttpContext
        {
            Request = { Method = HttpMethods.Get, Path = "/test", Headers = { ["test-header"] = "test" }, },
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.Invoke(httpContext));
        mockMonitoringService.Verify(a => a.ReportError(stubException, It.IsAny<Dictionary<string, string>>()), Times.Once);
    }
    [Fact]
    public async Task RequestLoggingMiddleware_WhenJwtTokenContainsClientId_AddsClientIdToLogTags()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var mockMonitoringService = new Mock<IMonitoringService>();
        var sut = new RequestLoggingMiddleware(
            async (innerHttpContext) => await Task.CompletedTask,
            mockLogger.Object,
            mockMonitoringService.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjY3REQxQjkwMDA5QjcwMjUzMTg3ODRGQzc0RTIxOTRGNTc0QThFQzUiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJaOTBia0FDYmNDVXhoNFQ4ZE9JWlQxZEtqc1UifQ.eyJuYmYiOjE3NDc2MzcyNDgsImV4cCI6MTc0NzY0MDg0OCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzL3Jlc291cmNlcyIsImNsaWVudF9pZCI6ImxvY2FsX2NhbGxlciIsImp0aSI6ImE2elZBODdCNmtKWnNoaW9tTWpfdkEiLCJzY29wZSI6Inhlcm9fY29sbGVjdGluZy1wYXltZW50c19wYXltZW50LXJlcXVlc3Qtc2VydmljZS5jcmVhdGUifQ.Nx3IU4ovZpMIGCtjo_g_PM224n6z6H_80sMTXdWSvz-uR_XQnodVF3DIBvcybYVx97E695_pqiwmGOhTi_oSiu-lDN0sS7W9KAvyrZle0-SKqgdIrhW3OY17kZbimwD_wMo-vCniCmc7ycrcpqWzYFNf-4HZJic16OD7t-qc8TFq6uXlx0P8PYn_4Tbxxsfdew_Io7a_g5osZarNCjGRWypUajLkYPG9uZ9VoLUFdYNrBNMbZDMlem5API5wL98HqGNFuH518iXCOWShPYntGCo81vaBIEc2VijdFO1KVq7aK9zXSOoShlo6-j6Rxm3ukBku8RNY4zM7AnYaRGGJ9rvmGEcfT5_4q3T_wkVxPyeg3ImW4MpyFJV6lI2Hqzbo8qCPP1jPRcFZ6FK49SsHgpTYSCuqJD8UyBICLFwNcfBK5zVft9CBwuvvEdT0kpyTVP0YkbRMThgULhM5kNcnXPaZT71uoaVVcRdbul9_FZt3_Qc-3bZQcol9qXUXWncWrNLPlhahMvwIQxZTw5lL7zJPzimNi3kp9hj_MoeTUrjD1tsfV0whSFfLjkHiTPCjDjv9Ovka3IUZ0jmRJjj8b_F6cqhtX5TB3gY8cmzwe2gHA78m8fStT0Q3D2Bc4WOBuuoKePLCzDoSVjtZwMeEWDEsOqM46nYjpqOlHGXqax4";

        // Act
        await sut.Invoke(httpContext);

        // Assert
        mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey("Xero-Client-Name") && tags["Xero-Client-Name"] == "local_caller")), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenAuthHeaderAndSchemaAllLowerCaseContainsClientId_AddsClientIdToLogTags()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var mockMonitoringService = new Mock<IMonitoringService>();
        var sut = new RequestLoggingMiddleware(
            async (innerHttpContext) => await Task.CompletedTask,
            mockLogger.Object,
            mockMonitoringService.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjY3REQxQjkwMDA5QjcwMjUzMTg3ODRGQzc0RTIxOTRGNTc0QThFQzUiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJaOTBia0FDYmNDVXhoNFQ4ZE9JWlQxZEtqc1UifQ.eyJuYmYiOjE3NDc2MzcyNDgsImV4cCI6MTc0NzY0MDg0OCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzL3Jlc291cmNlcyIsImNsaWVudF9pZCI6ImxvY2FsX2NhbGxlciIsImp0aSI6ImE2elZBODdCNmtKWnNoaW9tTWpfdkEiLCJzY29wZSI6Inhlcm9fY29sbGVjdGluZy1wYXltZW50c19wYXltZW50LXJlcXVlc3Qtc2VydmljZS5jcmVhdGUifQ.Nx3IU4ovZpMIGCtjo_g_PM224n6z6H_80sMTXdWSvz-uR_XQnodVF3DIBvcybYVx97E695_pqiwmGOhTi_oSiu-lDN0sS7W9KAvyrZle0-SKqgdIrhW3OY17kZbimwD_wMo-vCniCmc7ycrcpqWzYFNf-4HZJic16OD7t-qc8TFq6uXlx0P8PYn_4Tbxxsfdew_Io7a_g5osZarNCjGRWypUajLkYPG9uZ9VoLUFdYNrBNMbZDMlem5API5wL98HqGNFuH518iXCOWShPYntGCo81vaBIEc2VijdFO1KVq7aK9zXSOoShlo6-j6Rxm3ukBku8RNY4zM7AnYaRGGJ9rvmGEcfT5_4q3T_wkVxPyeg3ImW4MpyFJV6lI2Hqzbo8qCPP1jPRcFZ6FK49SsHgpTYSCuqJD8UyBICLFwNcfBK5zVft9CBwuvvEdT0kpyTVP0YkbRMThgULhM5kNcnXPaZT71uoaVVcRdbul9_FZt3_Qc-3bZQcol9qXUXWncWrNLPlhahMvwIQxZTw5lL7zJPzimNi3kp9hj_MoeTUrjD1tsfV0whSFfLjkHiTPCjDjv9Ovka3IUZ0jmRJjj8b_F6cqhtX5TB3gY8cmzwe2gHA78m8fStT0Q3D2Bc4WOBuuoKePLCzDoSVjtZwMeEWDEsOqM46nYjpqOlHGXqax4";

        // Act
        await sut.Invoke(httpContext);

        // Assert
        mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey("Xero-Client-Name") && tags["Xero-Client-Name"] == "local_caller")), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestHasXeroClientNameAndAuthHeader_SendsLogsWithClientIdAsXeroClientNameHeaderTag()
    {
        // Arrange
        var xeroClientName = "Xero-Client-Name";
        var clientNameHeaderValue = "CustomValue";
        var stubHttpContext = new DefaultHttpContext
        {
            Request = { Headers = { [xeroClientName] = clientNameHeaderValue } }
        };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        stubHttpContext.Request.Headers.Authorization = "bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjY3REQxQjkwMDA5QjcwMjUzMTg3ODRGQzc0RTIxOTRGNTc0QThFQzUiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJaOTBia0FDYmNDVXhoNFQ4ZE9JWlQxZEtqc1UifQ.eyJuYmYiOjE3NDc2MzcyNDgsImV4cCI6MTc0NzY0MDg0OCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzL3Jlc291cmNlcyIsImNsaWVudF9pZCI6ImxvY2FsX2NhbGxlciIsImp0aSI6ImE2elZBODdCNmtKWnNoaW9tTWpfdkEiLCJzY29wZSI6Inhlcm9fY29sbGVjdGluZy1wYXltZW50c19wYXltZW50LXJlcXVlc3Qtc2VydmljZS5jcmVhdGUifQ.Nx3IU4ovZpMIGCtjo_g_PM224n6z6H_80sMTXdWSvz-uR_XQnodVF3DIBvcybYVx97E695_pqiwmGOhTi_oSiu-lDN0sS7W9KAvyrZle0-SKqgdIrhW3OY17kZbimwD_wMo-vCniCmc7ycrcpqWzYFNf-4HZJic16OD7t-qc8TFq6uXlx0P8PYn_4Tbxxsfdew_Io7a_g5osZarNCjGRWypUajLkYPG9uZ9VoLUFdYNrBNMbZDMlem5API5wL98HqGNFuH518iXCOWShPYntGCo81vaBIEc2VijdFO1KVq7aK9zXSOoShlo6-j6Rxm3ukBku8RNY4zM7AnYaRGGJ9rvmGEcfT5_4q3T_wkVxPyeg3ImW4MpyFJV6lI2Hqzbo8qCPP1jPRcFZ6FK49SsHgpTYSCuqJD8UyBICLFwNcfBK5zVft9CBwuvvEdT0kpyTVP0YkbRMThgULhM5kNcnXPaZT71uoaVVcRdbul9_FZt3_Qc-3bZQcol9qXUXWncWrNLPlhahMvwIQxZTw5lL7zJPzimNi3kp9hj_MoeTUrjD1tsfV0whSFfLjkHiTPCjDjv9Ovka3IUZ0jmRJjj8b_F6cqhtX5TB3gY8cmzwe2gHA78m8fStT0Q3D2Bc4WOBuuoKePLCzDoSVjtZwMeEWDEsOqM46nYjpqOlHGXqax4";


        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey("Xero-Client-Name") && tags["Xero-Client-Name"] == "local_caller")), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenAuthHeaderAndSchemaButInvalidToken_AddsClientNameToLogTags()
    {
        // Arrange
        var xeroClientName = "Xero-Client-Name";
        var clientNameHeaderValue = "CustomValue";
        var stubHttpContext = new DefaultHttpContext
        {
            Request = { Headers = { [xeroClientName] = clientNameHeaderValue } }
        };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        stubHttpContext.Request.Headers.Authorization = "bearer ey";


        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey("Xero-Client-Name") && tags["Xero-Client-Name"] == "CustomValue")), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestHasAuthHeaderButTokenDoesNotHaveClientId_SendsLogsWithXeroClientNameHeaderValue()
    {
        // Arrange
        var xeroClientName = "Xero-Client-Name";
        var clientNameHeaderValue = "CustomValue";
        var stubHttpContext = new DefaultHttpContext
        {
            Request = { Headers = { [xeroClientName] = clientNameHeaderValue } }
        };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        stubHttpContext.Request.Headers.Authorization = "bearer eyJhbGciOiJub25lIn0.eyJuYmYiOjE3NDc2MzcyNDgsImV4cCI6MTc0NzY0MDg0OCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAzL3Jlc291cmNlcyIsImp0aSI6ImE2elZBODdCNmtKWnNoaW9tTWpfdkEiLCJzY29wZSI6Inhlcm9fY29sbGVjdGluZy1wYXltZW50c19wYXltZW50LXJlcXVlc3Qtc2VydmljZS5jcmVhdGUifQ.";


        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey("Xero-Client-Name") && tags["Xero-Client-Name"] == "CustomValue")), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestContainsXeroClientNameHeader_LogsXeroClientNameHeaderValue()
    {
        // Arrange
        var xeroClientName = "Xero-Client-Name";
        var clientNameHeaderValue = "CustomValue";
        var stubHttpContext = new DefaultHttpContext
        {
            Request = { Headers = { [xeroClientName] = clientNameHeaderValue } }
        };
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(stubHttpContext);

        // Assert
        mockLogger.Verify(l => l.BeginScope(It.Is<Dictionary<string, string>>(tags =>
            tags.ContainsKey(xeroClientName) && tags[xeroClientName] == clientNameHeaderValue)), Times.Once);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_WhenRequestHasHoXeroClientNameHeader_SendsLogsWithoutXeroClientNameHeaderTag()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var sut = new RequestLoggingMiddleware(Mock.Of<RequestDelegate>(), mockLogger.Object, Mock.Of<IMonitoringService>());

        // Act
        await sut.Invoke(new DefaultHttpContext());

        // Assert
        mockLogger.Verify(l => l.BeginScope(It.Is<Dictionary<string, object>>(tags => tags.ContainsKey("Xero-Client-Name"))), Times.Never);
    }
}
