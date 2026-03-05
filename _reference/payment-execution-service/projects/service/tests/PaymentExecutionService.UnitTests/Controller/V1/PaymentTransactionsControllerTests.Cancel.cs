using System;
using System.Collections.Generic;
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

public class PaymentTransactionsControllerTests_Cancel
{
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PaymentsController _sut;
    private readonly Guid _mockXeroTenantId = Guid.NewGuid();
    private readonly Guid _mockXeroCorrelationId = Guid.NewGuid();

    public PaymentTransactionsControllerTests_Cancel()
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
    public async Task GivenValidRequest_WhenCancelIsCalled_ThenMediatorCalledWithExpectedPayload(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        //Act
        await _sut.Cancel(paymentRequestId, payload);

        //Assert
        _mockMediator.Verify(m => m.Send(It.Is<SynchronousCancellationCommand>(
            cmd => cmd.PaymentRequestId == paymentRequestId &&
                   cmd.CancellationReason == payload.CancellationReason &&
                   cmd.CorrelationId == _mockXeroCorrelationId.ToString() &&
                   cmd.TenantId == _mockXeroTenantId.ToString()),
    It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsResultOk_WhenCancelIsCalled_ThenReturnsNoContent(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        //Act
        var result = await _sut.Cancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = result as NoContentResult;
        noContentResult!.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsPaymentTransactionNotCancellableError_WhenCancelIsCalled_ThenReturnsBadRequestWithProblemDetails(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        var errorMessage = "Payment transaction is not cancellable";
        var errorCode = ErrorConstants.ErrorCode.ExecutionCancellationError;
        var providerErrorCode = "some-provider-error-message";
        var mockedError = new PaymentExecutionError(errorMessage, ErrorType.PaymentTransactionNotCancellable, errorCode,
            providerErrorCode);

        var mockedResult = Result.Fail(mockedError);

        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Cancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = badRequestResult.Value as ProblemDetailsExtended;
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = nameof(HttpStatusCode.BadRequest),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Detail = errorMessage,
            ErrorCode = errorCode,
            Extensions = new Dictionary<string, object?>
            {
                { "errors", new List<string> { errorMessage } }
            },
            ProviderErrorCode = providerErrorCode
        };
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsPaymentTransactionNotFoundError_WhenCancelIsCalled_ThenReturnsNotFound(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        var errorMessage = "Payment transaction not found";
        var errorCode = ErrorConstants.ErrorCode.GenericExecutionError;
        var mockedError = new PaymentExecutionError(errorMessage,
            ErrorType.PaymentTransactionNotFound,
            errorCode);
        var mockedResult = Result.Fail(mockedError);

        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Cancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var problemDetails = objectResult.Value as ProblemDetailsExtended;
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.NotFound,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
            Title = nameof(HttpStatusCode.NotFound),
            ErrorCode = errorCode,
        };

        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsErrorWithTypeDependencyTransientError_WhenCancelIsCalled_ThenReturnsExpectedProblemDetails(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        var errorMessage = "Payment transaction not found";
        var mockProviderErrorCode = "some-provider-error-code";
        var errorCode = ErrorConstants.ErrorCode.GenericExecutionError;
        var mockedError = new PaymentExecutionError(errorMessage,
            ErrorType.DependencyTransientError,
            errorCode,
            mockProviderErrorCode);
        var mockedResult = Result.Fail(mockedError);

        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Cancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var problemDetails = objectResult.Value as ProblemDetailsExtended;
        var expectedProblemDetails = new ProblemDetailsExtended
        {
            Status = (int)HttpStatusCode.ServiceUnavailable,
            Title = nameof(HttpStatusCode.ServiceUnavailable),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",
            Detail = errorMessage,
            ErrorCode = errorCode,
            ProviderErrorCode = mockProviderErrorCode,
            Extensions = new Dictionary<string, object?>
            {
                { "errors", new List<string> { errorMessage } }
            },
        };
        problemDetails.Should().BeEquivalentTo(expectedProblemDetails);
    }

    [Theory, AutoData]
    public async Task GivenGenericResultFailure_WhenCancelIsCalled_ThenReturnsExpectedResponse(
        Guid paymentRequestId, CancelPayload payload)
    {
        //Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<SynchronousCancellationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Something has happened! :("));

        //Act
        var result = await _sut.Cancel(paymentRequestId, payload);

        //Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        ((ProblemDetailsExtended)objectResult.Value!).Detail.Should()
            .Contain("Unexpected error occurred while processing the request. Please try again later.");
    }
}
