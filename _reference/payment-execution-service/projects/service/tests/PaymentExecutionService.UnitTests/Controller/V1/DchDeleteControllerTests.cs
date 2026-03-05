using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models.Errors;
using PaymentExecutionService.Controllers.V1;
using PaymentExecutionService.Models;
using Xunit;

namespace PaymentExecutionService.UnitTests.Controller.V1;

public class DchDeleteControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DchDeleteController _controller;

    public DchDeleteControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new DchDeleteController(Mock.Of<ILogger<DchDeleteController>>(), _mediatorMock.Object);
    }

    [Fact]
    public async Task GivenDchDeletePayload_WhenOrgIdNotRecognised_ThenNotFoundIsReturned()
    {
        // Arrange
        var payload = new DchDeletePayload { IdToDelete = Guid.NewGuid() };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteByOrgCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new PaymentTransactionNotFoundError()));

        // Act
        var result = await _controller.DchDelete(payload);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GivenDchDeletePayload_WhenCommandHandlerSucceeds_ThenOkIsReturned()
    {
        // Arrange
        var payload = new DchDeletePayload { IdToDelete = Guid.NewGuid() };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteByOrgCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.DchDelete(payload);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GivenDchDeletePayload_WhenUnrecognisedFailureReturnedFromCommand_ThenUnknownBadRequestIsReturned()
    {
        // Arrange
        var payload = new DchDeletePayload { IdToDelete = Guid.NewGuid() };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteByOrgCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Unrecognized error"));

        // Act
        var result = await _controller.DchDelete(payload);

        // Assert
        var badRequestResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }
}
