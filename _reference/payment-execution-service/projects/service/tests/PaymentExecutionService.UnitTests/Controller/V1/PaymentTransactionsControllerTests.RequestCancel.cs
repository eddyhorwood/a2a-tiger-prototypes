using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecutionService.Controllers.V1;
using PaymentExecutionService.Models;
using Xunit;
using static Xero.Accelerators.Api.Core.Constants;

namespace PaymentExecutionService.UnitTests.Controller.V1;

public class PaymentTransactionsControllerTestsRequestCancel
{
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PaymentsController _sut;
    private readonly Guid _mockXeroTenantId = Guid.NewGuid();
    private readonly Guid _mockXeroCorrelationId = Guid.NewGuid();

    public PaymentTransactionsControllerTestsRequestCancel()
    {

        _sut = new PaymentsController(_mockMediator.Object, _mapper.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaders.XeroCorrelationId] = _mockXeroCorrelationId.ToString();
        httpContext.Request.Headers[HttpHeaders.XeroTenantId] = _mockXeroTenantId.ToString();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Theory, AutoData]
    public async Task GivenMapperThrowsException_WhenRequestCancelIsCalled_ThenExceptionPropagated(
        Guid paymentRequestId, RequestCancelPayload payload)
    {
        //Arrange
        var mockedExceptionMessage = "something has happened!";
        _mapper.Setup(m => m.Map<RequestCancelCommand>(payload))
            .Throws(new Exception(mockedExceptionMessage));

        //Act
        var act = async () => await _sut.RequestCancel(paymentRequestId, payload);

        //Assert
        await act.Should().ThrowAsync<Exception>().WithMessage(mockedExceptionMessage);
    }

    [Theory, AutoData]
    public async Task GivenValidRequestAndSuccessfulMapping_WhenRequestCancelIsCalled_ThenMediatorCalledWithExpectedPayload(
        Guid paymentRequestId, RequestCancelPayload payload, RequestCancelCommand mockedCommand)
    {
        //Arrange
        _mapper.Setup(m => m.Map<RequestCancelCommand>(payload))
            .Returns(mockedCommand);
        _mockMediator.Setup(m => m.Send(It.IsAny<RequestCancelCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        //Act
        await _sut.RequestCancel(paymentRequestId, payload);

        //Assert
        _mockMediator.Verify(m => m.Send(It.Is<RequestCancelCommand>(
            comm => comm.PaymentRequestId == paymentRequestId &&
                    comm.CancellationReason == mockedCommand.CancellationReason &&
                    comm.XeroCorrelationId == _mockXeroCorrelationId &&
                    comm.XeroTenantId == _mockXeroTenantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsResultOk_WhenRequestCancelIsCalled_ThenReturnsAccepted(
        Guid paymentRequestId, RequestCancelPayload payload, RequestCancelCommand mockedCommand)
    {
        //Arrange
        _mapper.Setup(m => m.Map<RequestCancelCommand>(payload))
            .Returns(mockedCommand);
        _mockMediator.Setup(m => m.Send(It.IsAny<RequestCancelCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        //ACt
        var response = await _sut.RequestCancel(paymentRequestId, payload);

        //Assert
        response.Should().BeOfType<AcceptedResult>();
    }

    [Theory, AutoData]
    public async Task GivenGenericResultFailure_WhenRequestCancelIsCalled_ThenReturns500(
        Guid paymentRequestId, RequestCancelPayload payload, RequestCancelCommand mockedCommand)
    {
        //Arrange
        _mapper.Setup(m => m.Map<RequestCancelCommand>(payload))
            .Returns(mockedCommand);
        _mockMediator.Setup(m => m.Send(It.IsAny<RequestCancelCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("something has happened!"));

        //Act
        var result = await _sut.RequestCancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCode = ((ObjectResult)result).StatusCode;
        statusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineAutoData(ErrorType.PaymentTransactionNotCancellable, HttpStatusCode.BadRequest)]
    [InlineAutoData(ErrorType.PaymentTransactionNotFound, HttpStatusCode.NotFound)]
    public async Task
        GivenMediatorReturnsResultFailWithErrorType_WhenRequestCancelledIsCalled_ThenReturnsExpectedStatusCode(
            ErrorType mockedErrorType, HttpStatusCode expectedStatusCode, RequestCancelPayload payload, RequestCancelCommand mockedCommand)
    {
        //Arrange
        var mockedError = new PaymentExecutionError("error message", mockedErrorType,
            "error-code");
        var mockedResult = Result.Fail(mockedError);
        _mapper.Setup(m => m.Map<RequestCancelCommand>(payload))
            .Returns(mockedCommand);
        _mockMediator.Setup(m => m.Send(It.IsAny<RequestCancelCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.RequestCancel(Guid.NewGuid(), payload);

        //Assert
        var statusCode = ((ObjectResult)result).StatusCode;
        statusCode.Should().Be((int)expectedStatusCode);
    }
}
