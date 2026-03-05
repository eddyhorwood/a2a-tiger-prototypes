using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
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

namespace PaymentExecutionService.UnitTests.Controller.V1;

public class SubmitStripeControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SubmitStripeController _sut;

    public SubmitStripeControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockMapper = new Mock<IMapper>();

        _sut = new SubmitStripeController(_mockMediator.Object, _mockMapper.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroCorrelationId] = Guid.NewGuid().ToString();
        httpContext.Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId] = Guid.NewGuid().ToString();
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Theory]
    [MemberData(nameof(CreateValidSubmitStripeRequest))]
    public async Task GivenValidInput_WhenSubmitPayment_ThenShouldReturnOkResult(SubmitStripeRequest request)
    {
        //Arrange
        var expectedPaymentIntentId = "pi_12345";
        var expectedClientSecret = "top-secret-secret";

        _mockMapper
            .Setup(m => m.Map<SubmitStripePaymentCommand>(It.IsAny<SubmitStripeRequest>()))
            .Returns(new SubmitStripePaymentCommand
            {
                PaymentRequestId = Guid.NewGuid(),
                XeroCorrelationId = Guid.NewGuid().ToString(),
                XeroTenantId = Guid.NewGuid().ToString()
            });

        _mockMediator
            .Setup(m => m.Send(It.IsAny<SubmitStripePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new SubmitStripePaymentCommandResponse()
            {
                PaymentIntentId = expectedPaymentIntentId,
                ClientSecret = expectedClientSecret
            }))
            .Verifiable();

        //Act
        var result = await _sut.SubmitPayment(request);

        //Assert
        Assert.IsType<OkObjectResult>(result);
        var castedResultObject = (SubmitStripePaymentCommandResponse)((OkObjectResult)result).Value!;
        Assert.Equal(expectedPaymentIntentId, castedResultObject.PaymentIntentId);
        Assert.Equal(expectedClientSecret, castedResultObject.ClientSecret);
    }

    [Theory]
    [AutoData]
    public async Task GivenMediatorThrowsException_WhenSubmitPayment_ThenExceptionShouldBeRaised(SubmitStripeRequest request)
    {
        //Arrange
        _mockMapper
            .Setup(m => m.Map<SubmitStripePaymentCommand>(It.IsAny<SubmitStripeRequest>()))
            .Returns(new SubmitStripePaymentCommand
            {
                PaymentRequestId = Guid.NewGuid(),
                XeroCorrelationId = Guid.NewGuid().ToString(),
                XeroTenantId = Guid.NewGuid().ToString()
            });

        _mockMediator
            .Setup(m => m.Send(It.IsAny<SubmitStripePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("something terrible happened"));

        await Assert.ThrowsAsync<Exception>(async () => await _sut.SubmitPayment(request));
    }

    [Theory]
    [AutoData]
    public async Task GivenMediatorReturnsResultFailWithErrorOfTypePaymentFailed_WhenSubmitPayment_ThenReturnsExpectedProblemDetails(SubmitStripeRequest request)
    {
        //Arrange
        var expectedMessage = "Error message back from Stripe";
        var expectedProviderErrorCode = "invalid-something-code";
        var mockedError = new PaymentExecutionError(expectedMessage,
            ErrorType.PaymentFailed,
            ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed,
            expectedProviderErrorCode);
        var mockedResult = Result.Fail(mockedError);
        var expectedProblemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.PaymentRequired,
            Title = nameof(HttpStatusCode.PaymentRequired),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.2",
            Detail = expectedMessage,
            ErrorCode = ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed,
            ProviderErrorCode = expectedProviderErrorCode,
            Extensions = new Dictionary<string, object?>()
            {
                { "errors", new List<string>() { expectedMessage } }
            }
        };

        _mockMapper
            .Setup(m => m.Map<SubmitStripePaymentCommand>(It.IsAny<SubmitStripeRequest>()))
            .Returns(new SubmitStripePaymentCommand
            {
                PaymentRequestId = Guid.NewGuid(),
                XeroCorrelationId = Guid.NewGuid().ToString(),
                XeroTenantId = Guid.NewGuid().ToString()
            });

        _mockMediator
            .Setup(m => m.Send(It.IsAny<SubmitStripePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        var response = await _sut.SubmitPayment(request);

        Assert.IsType<ObjectResult>(response);
        var castedResult = response as ObjectResult;
        Assert.Equal((int)HttpStatusCode.PaymentRequired, castedResult?.StatusCode);
        Assert.Equivalent(expectedProblemDetails, castedResult?.Value);
    }

    [Theory]
    [InlineAutoData(ErrorType.ClientError)]
    [InlineAutoData(ErrorType.BadPaymentRequest)]
    public async Task GivenMediatorReturnsResultFailWithErrorOfTypeClientErrorOrBadPaymentRequest_WhenSubmitPayment_ThenReturnsExpectedProblemDetails(ErrorType errorType, SubmitStripeRequest request)
    {
        //Arrange
        var expectedMessage = "Error message back from some error";
        var mockedError = new PaymentExecutionError(expectedMessage,
            errorType,
            ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed);
        var mockedResult = Result.Fail(mockedError);
        var expectedProblemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = nameof(HttpStatusCode.BadRequest),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Detail = mockedError.Message,
            ErrorCode = mockedError.GetErrorCode(),
            ProviderErrorCode = mockedError.GetProviderErrorCode(),
            Extensions = new Dictionary<string, object?>()
            {
                { "errors", new List<string>() { expectedMessage } }
            }
        };

        _mockMapper
            .Setup(m => m.Map<SubmitStripePaymentCommand>(It.IsAny<SubmitStripeRequest>()))
            .Returns(new SubmitStripePaymentCommand
            {
                PaymentRequestId = Guid.NewGuid(),
                XeroCorrelationId = Guid.NewGuid().ToString(),
                XeroTenantId = Guid.NewGuid().ToString()
            });

        _mockMediator
            .Setup(m => m.Send(It.IsAny<SubmitStripePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        var response = await _sut.SubmitPayment(request);

        Assert.IsType<BadRequestObjectResult>(response);
        var castedResult = response as BadRequestObjectResult;
        Assert.Equal((int)HttpStatusCode.BadRequest, castedResult?.StatusCode);
        Assert.Equivalent(expectedProblemDetails, castedResult?.Value);
    }

    [Theory, AutoData]
    public async Task GivenMediatorReturnsGenericResultFail_WhenSubmitPayment_ThenReturns500(SubmitStripeRequest request)
    {
        //Arrange
        _mockMapper
            .Setup(m => m.Map<SubmitStripePaymentCommand>(It.IsAny<SubmitStripeRequest>()))
            .Returns(new SubmitStripePaymentCommand
            {
                PaymentRequestId = Guid.NewGuid(),
                XeroCorrelationId = Guid.NewGuid().ToString(),
                XeroTenantId = Guid.NewGuid().ToString()
            });

        var mockedResult = Result.Fail("Some non Payment Execution error");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<SubmitStripePaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResult);

        var response = await _sut.SubmitPayment(request);

        response.Should().BeOfType<ObjectResult>();
        var castedResult = (ObjectResult)response;
        castedResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        ((ProblemDetailsExtended)castedResult.Value!).Detail.Should().Be(
            "Unexpected error occurred while processing the request. Please try again later.");
    }

    public static TheoryData<SubmitStripeRequest> CreateValidSubmitStripeRequest()
    {
        var fixture = new Fixture();
        var data = new TheoryData<SubmitStripeRequest>();

        var submitStripeRequest = fixture.Create<SubmitStripeRequest>();
        submitStripeRequest.PaymentMethodsMadeAvailable = null;
        data.Add(submitStripeRequest);

        var submitStripeRequestTwo = fixture.Create<SubmitStripeRequest>();
        submitStripeRequestTwo.PaymentMethodId = null;
        data.Add(submitStripeRequestTwo);

        return data;
    }
}

