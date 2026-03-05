using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;

namespace PaymentExecution.Domain.UnitTests.Commands;

public class SynchronousCancellationCommandTests
{
    private readonly SynchronousCancellationCommandHandler _sut;
    private readonly Mock<ICancelDomainService> _mockDomainService = new();
    private readonly Mock<ICancellationValidationService> _mockCancellationValidationService = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<ILogger<SynchronousCancellationCommandHandler>> _mockLogger = new();

    public SynchronousCancellationCommandTests()
    {
        _sut = new SynchronousCancellationCommandHandler(
            _mockDomainService.Object,
            _mockCancellationValidationService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Theory, AutoData]
    public async Task GivenHandleGetPaymentTransactionRecordReturnsFailed_WhenHandleInvoked_ThenReturnsResultFailed(
        SynchronousCancellationCommand mockCommand)
    {
        //Arrange
        var expectedError = "Payment transaction not found";
        var mockedResult = Result.Fail(expectedError);
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Fail<CancellationRequest>(expectedError));

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedResult);
    }

    [Theory, AutoData]
    public async Task GivenPaymentAlreadyCancelled_WhenHandleInvoked_ThenReturnsResultOk(
        SynchronousCancellationCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancellationRequest.Status))
            .Returns(true);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeEquivalentTo(Result.Ok());
        _mockCancellationValidationService.Verify();
        _mockDomainService.Verify();
    }

    [Theory, AutoData]
    public async Task GivenPaymentIsNotCancellable_WhenHandleInvoked_ThenReturnsExpectedError(
        SynchronousCancellationCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancellationRequest.Status))
            .Returns(false);
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancellable(mockCancellationRequest))
            .Returns(false);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        var expectedError = new PaymentExecutionError("Payment Request is not cancellable",
            ErrorType.PaymentTransactionNotCancellable, ErrorConstants.ErrorCode.ExecutionCancellationError);
        result.Should().BeEquivalentTo(Result.Fail(expectedError));
        _mockDomainService.Verify();
    }

    [Theory, AutoData]
    public async Task GivenMappingToCancelPaymentRequestThrows_WhenHandleInvoked_ThenPropagatesException(
        SynchronousCancellationCommand mockCommand, CancellationRequest mockCancellationRequest)
    {
        //Arrange
        var mockException = new Exception("Mapping has gone totally wrong!");
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancellationRequest.Status))
            .Returns(false);
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancellable(mockCancellationRequest))
            .Returns(true);
        _mockMapper.Setup(m => m.Map<CancelPaymentRequest>(mockCommand))
            .Throws(mockException);

        //Act
        var act = () => _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        await act.Should().ThrowExactlyAsync<Exception>().WithMessage(mockException.Message);
    }

    [Theory, AutoData]
    public async Task GivenCancellableAndHandleCancellationAsyncReturnsResultFail_WhenHandleInvoked_ThenPropagatesResultFail(
        SynchronousCancellationCommand mockCommand, CancellationRequest mockCancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedResult = Result.Fail("Something happened trying to cancel the payment");
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancellationRequest.Status))
            .Returns(false);
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancellable(mockCancellationRequest))
            .Returns(true);
        _mockMapper.Setup(m => m.Map<CancelPaymentRequest>(mockCommand))
            .Returns(mockCancelPaymentRequest);
        _mockDomainService.Setup(m => m.HandleSyncCancellationAsync(mockCancellationRequest.ProviderType, mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().Be(mockedResult);
    }

    [Theory, AutoData]
    public async Task GivenCancellableAndHandleCancellationAsyncReturnsResultOk_WhenHandleInvoked_ThenPropagatesResultOk(
        SynchronousCancellationCommand mockCommand, CancellationRequest mockCancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedResult = Result.Ok();
        _mockDomainService.Setup(m => m.HandleGetPaymentTransactionRecordAsync(mockCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockCancellationRequest));
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancellationRequest.Status))
            .Returns(false);
        _mockCancellationValidationService.Setup(m => m.IsPaymentTransactionCancellable(mockCancellationRequest))
            .Returns(true);
        _mockMapper.Setup(m => m.Map<CancelPaymentRequest>(mockCommand))
            .Returns(mockCancelPaymentRequest);
        _mockDomainService.Setup(m => m.HandleSyncCancellationAsync(mockCancellationRequest.ProviderType, mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.Handle(mockCommand, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().Be(mockedResult);
    }
}
