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
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Queries;
using PaymentExecutionService.Controllers.V1;
using PaymentExecutionService.Models.Response;
using Xunit;
using static Xero.Accelerators.Api.Core.Constants;

namespace PaymentExecutionService.UnitTests.Controller.V1;

public class PaymentTransactionsControllerTestsGetProviderState
{
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly PaymentsController _sut;
    private readonly Guid _mockXeroTenantId = Guid.NewGuid();
    private readonly Guid _mockXeroCorrelationId = Guid.NewGuid();

    public PaymentTransactionsControllerTestsGetProviderState()
    {
        _sut = new PaymentsController(_mockMediator.Object, _mockMapper.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[HttpHeaders.XeroCorrelationId] = _mockXeroCorrelationId.ToString();
        httpContext.Request.Headers[HttpHeaders.XeroTenantId] = _mockXeroTenantId.ToString();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Theory, AutoData]
    public async Task GivenMediatrReturnsResultOk_WhenGetProviderStateIsCalled_ThenOkObjectResultIsReturned(
        ProviderState mockProviderState, GetProviderStateResponse mockedMappedResponse)
    {
        //Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedMediatrQueryResponse = new GetProviderStateQueryResponse
        {
            ProviderState = mockProviderState
        };
        _mockMediator.Setup(m => m.Send(
                It.Is<GetProviderStateQuery>(q => q.PaymentRequestId == paymentRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(mockedMediatrQueryResponse));
        _mockMapper.Setup(m => m.Map<GetProviderStateResponse>(mockProviderState))
            .Returns(mockedMappedResponse);

        //Act
        var result = await _sut.GetProviderState(paymentRequestId);

        //Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().NotBeNull();
        var returnedProviderState = (GetProviderStateResponse)okResult.Value;
        returnedProviderState!.Should().BeEquivalentTo(mockedMappedResponse);
    }

    [Fact]
    public async Task GivenMediatrReturnsGenericResultFail_WhenGetProviderStateIsCalled_ThenReturnsNonSuccessfulStatusCode()
    {
        //Arrange
        var paymentRequestId = Guid.NewGuid();
        _mockMediator.Setup(m => m.Send(
                It.Is<GetProviderStateQuery>(q => q.PaymentRequestId == paymentRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Something has happened!"));
        var expectedErrorDetail = "Unexpected error occurred while processing the request. Please try again later.";

        //Act
        var result = await _sut.GetProviderState(paymentRequestId);

        //Assert
        var objectResult = ((ObjectResult)result);
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var responseContent = (ProblemDetailsExtended)objectResult.Value!;
        responseContent.Detail.Should().Contain(expectedErrorDetail);
        responseContent.ErrorCode.Should().Be(ErrorConstants.ErrorCode.ExecutionUnexpectedError);
    }

    [Fact]
    public async Task GivenMediatrReturnsErrorWithTypeFailedDependency_WhenGetProviderStateIsCalled_ThenReturnsExpectedResponse()
    {
        //Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedErrorMessage = "something has happened at Stripe!";
        var mockedErrorCode = ErrorConstants.ErrorCode.ExecutionGetProviderStateError;
        var mockedProviderErrorCode = "SomeErrorCode";
        var mockedError = new PaymentExecutionError(
            mockedErrorMessage,
            ErrorType.FailedDependency,
            mockedErrorCode,
            mockedProviderErrorCode);
        _mockMediator.Setup(m => m.Send(
                It.Is<GetProviderStateQuery>(q => q.PaymentRequestId == paymentRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(mockedError));

        //Act
        var result = await _sut.GetProviderState(paymentRequestId);

        //Assert
        var objectResult = (ObjectResult)result;
        var expectedResponse = new ProblemDetailsExtended()
        {
            Status = StatusCodes.Status424FailedDependency,
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/424",
            Title = nameof(HttpStatusCode.FailedDependency),
            Detail = mockedErrorMessage,
            ErrorCode = mockedErrorCode,
            ProviderErrorCode = mockedProviderErrorCode,
            Extensions =
            {
                { "errors", new[] { mockedErrorMessage } }
            }
        };
        objectResult.Value.Should().BeEquivalentTo(expectedResponse);
        objectResult.StatusCode.Should().Be(StatusCodes.Status424FailedDependency);
    }

    [Fact]
    public async Task GivenMediatrReturnsErrorWithTypeDependencyTransientError_WhenGetProviderStateIsCalled_ThenReturnsExpectedResponse()
    {
        //Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedErrorMessage = "oh dear we are down in Stripe exe land!";
        var mockedErrorCode = ErrorConstants.ErrorCode.ExecutionGetProviderStateError;
        var mockedError = new PaymentExecutionError(
            mockedErrorMessage,
            ErrorType.DependencyTransientError,
            mockedErrorCode);
        _mockMediator.Setup(m => m.Send(
                It.Is<GetProviderStateQuery>(q => q.PaymentRequestId == paymentRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(mockedError));

        //Act
        var result = await _sut.GetProviderState(paymentRequestId);

        //Assert
        var objectResult = (ObjectResult)result;
        var expectedResponse = new ProblemDetailsExtended()
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4",
            Title = nameof(HttpStatusCode.ServiceUnavailable),
            Detail = mockedErrorMessage,
            ErrorCode = mockedErrorCode,
            Extensions =
            {
                { "errors", new[] { mockedErrorMessage } }
            }
        };
        objectResult.Value.Should().BeEquivalentTo(expectedResponse);
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }
}
