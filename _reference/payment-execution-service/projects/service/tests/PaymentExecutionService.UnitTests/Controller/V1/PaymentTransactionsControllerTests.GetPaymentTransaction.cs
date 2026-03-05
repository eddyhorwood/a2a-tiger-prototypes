using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Queries;
using PaymentExecutionService.Controllers.V1;
using PaymentExecutionService.Mapping;
using Xunit;
namespace PaymentExecutionService.UnitTests.Controller.V1;

public class PaymentTransactionsControllerTests_GetPaymentTransaction
{

    private readonly Mock<IMediator> _mockMediator;
    private readonly PaymentsController _controller;
    private readonly IMapper _mapper;


    public PaymentTransactionsControllerTests_GetPaymentTransaction()
    {
        _mockMediator = new Mock<IMediator>();
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ControllerToDomainMappingProfile>())
            .CreateMapper();
        _controller = new PaymentsController(_mockMediator.Object, _mapper);
    }


    [Fact]
    public async Task GivenValidInput_WhenGetPaymentTransactionIsCalled_ShouldReturn200WithResponse()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var expectedQueryResponse = new GetPaymentTransactionQueryResponse
        {
            PaymentRequestId = paymentRequestId,
            PaymentTransactionId = Guid.NewGuid(),
            Status = "Success",
            Fee = 100,
            FeeCurrency = "USD",
            ProviderType = "Stripe",
            ProviderServiceId = Guid.NewGuid()
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ReturnsAsync(Result.Ok(expectedQueryResponse));

        // Act
        var result = await _controller.GetPaymentTransaction(paymentRequestId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeOfType<GetPaymentTransactionQueryResponse>();
    }

    [Fact]
    public async Task GivenValidInputWithNotExistingRequestId_WhenGetPaymentTransactionIsCalled_ShouldReturn404()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.GetPaymentTransaction(paymentRequestId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var notFoundResult = (ObjectResult)result;
        notFoundResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenValidInputWithNotExistingRequestId_WhenGetPaymentTransactionIsCalled_ShouldReturnExpectedExtendedProblemDetails()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.GetPaymentTransaction(paymentRequestId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemDetails = (ProblemDetailsExtended)((ObjectResult)result).Value!;
        problemDetails.ErrorCode.Should().Be(ErrorConstants.ErrorCode.GenericExecutionError);
        problemDetails.ProviderErrorCode.Should().BeNull();
        problemDetails.Extensions.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenInValidInput_WhenGetPaymentTransactionIsCalled_ShouldReturn400()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ReturnsAsync(Result.Fail("Validation Error"));

        // Act
        var result = await _controller.GetPaymentTransaction(paymentRequestId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var badResult = (ObjectResult)result;
        badResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenInValidInput_WhenGetPaymentTransactionIsCalled_ShouldReturnExpectedExtendedProblemDetails()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ReturnsAsync(Result.Fail("Validation Error"));

        // Act
        var result = await _controller.GetPaymentTransaction(paymentRequestId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemDetails = (ProblemDetailsExtended)((ObjectResult)result).Value!;
        problemDetails.ErrorCode.Should().Be(ErrorConstants.ErrorCode.GenericExecutionError);
        problemDetails.ProviderErrorCode.Should().BeNull();
        problemDetails.Extensions["errors"].Should().BeEquivalentTo(
            new List<string> { "Validation Error" }
        );
    }

    [Fact]
    public async Task GivenExceptionThrownByMediatR_WhenGetPaymentTransactionIsCalled_ShouldThrowException()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), default))
            .ThrowsAsync(new Exception("Exception here"));

        // Act & Assert
        await _controller.Invoking(async controller => await controller.GetPaymentTransaction(paymentRequestId))
            .Should().ThrowAsync<Exception>();
    }
}
