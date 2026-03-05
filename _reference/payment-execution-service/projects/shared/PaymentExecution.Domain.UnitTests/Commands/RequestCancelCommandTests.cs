using AutoFixture.Xunit2;
using FluentAssertions;
using FluentResults;
using Moq;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;

namespace PaymentExecution.Domain.UnitTests.Commands;

public class RequestCancelCommandTests
{
    private readonly RequestCancelCommandHandler _sut;
    private readonly Mock<IRequestCancelDomainService> _mockedRequestCancelDomainService = new();

    public RequestCancelCommandTests()
    {
        _sut = new RequestCancelCommandHandler(_mockedRequestCancelDomainService.Object);
    }

    [Theory, AutoData]
    public async Task GivenHandlingGetPaymentTransactionByIdReturnsFailed_WhenHandleInvoked_ThenReturnsResultFailed(RequestCancelCommand mockCommand)
    {
        //Arrange
        _mockedRequestCancelDomainService.Setup(m => m.HandleGetCancellationRequestAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Fail("Error retrieving record"));

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenHandleGetPaymentTransactionReturnsOk_WhenHandleInvoked_ThenCallsHandleCancellationWithExpectedParameters(
        RequestCancelCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        _mockedRequestCancelDomainService.Setup(m => m.HandleGetCancellationRequestAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(mockCancellationRequest);
        _mockedRequestCancelDomainService.Setup(m => m.HandleRequestCancellationAsync(mockCancellationRequest,
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok());

        //Act
        await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        _mockedRequestCancelDomainService.Verify(m => m.HandleRequestCancellationAsync(
            mockCancellationRequest, mockCommand.CancellationReason, mockCommand.XeroTenantId, mockCommand.XeroCorrelationId), Times.Once);
    }


    [Theory, AutoData]
    public async Task GivenHandleCancellationReturnsResultFail_WhenHandleInvoked_ThenReturnsResultFail(
        RequestCancelCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        var mockedResult = Result.Fail("Cancellation failed");
        _mockedRequestCancelDomainService.Setup(m => m.HandleGetCancellationRequestAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(mockCancellationRequest);
        _mockedRequestCancelDomainService.Setup(m => m.HandleRequestCancellationAsync(mockCancellationRequest,
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.Should().BeEquivalentTo(mockedResult);
    }

    [Theory, AutoData]
    public async Task GivenHandleCancellationReturnsResultOk_WhenHandleInvoked_ThenReturnsResultOk(
        RequestCancelCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        _mockedRequestCancelDomainService.Setup(m => m.HandleGetCancellationRequestAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockedRequestCancelDomainService.Setup(m => m.HandleRequestCancellationAsync(mockCancellationRequest,
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
    }
}
