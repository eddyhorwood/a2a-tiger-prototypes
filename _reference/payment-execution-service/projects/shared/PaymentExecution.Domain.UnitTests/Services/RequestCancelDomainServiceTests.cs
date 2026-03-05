using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.SqsIntegrationClient.Service;

namespace PaymentExecution.Domain.UnitTests.Services;

public class RequestCancelDomainServiceTests
{
    private readonly RequestCancelDomainService _sut;
    private readonly Mock<ICancellationValidationService> _mockedCancellationValidationService = new();
    private readonly Mock<IPaymentTransactionRepository> _mockRepository = new();
    private readonly Mock<ILogger<RequestCancelDomainService>> _mockLogger = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<ICancelExecutionQueueService> _mockCancelExecutionQueueService = new();

    public RequestCancelDomainServiceTests()
    {
        _sut = new RequestCancelDomainService(
            _mockedCancellationValidationService.Object,
            _mockRepository.Object,
            _mockMapper.Object,
            _mockCancelExecutionQueueService.Object,
            _mockLogger.Object);
    }

    #region HandleGetCancellationRequestAsync
    [Theory, AutoData]
    public async Task GivenRepositoryReturnsPaymentTransactionDtoAndMapSuccessful_WhenHandleGetCancellationRequestAsyncInvoked_ThenReturnsResultWithExpectedPayload(
        PaymentTransactionDto mockedDto, CancellationRequest mockedCancellationRequest)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok((PaymentTransactionDto?)mockedDto));
        _mockMapper.Setup(m => m.Map<CancellationRequest>(mockedDto))
            .Returns(mockedCancellationRequest);

        //Act
        var result = await _sut.HandleGetCancellationRequestAsync(Guid.NewGuid());

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockedCancellationRequest);
    }

    [Fact]
    public async Task GivenRepositoryReturnsFailed_WhenHandleGetPaymentTransactionByIdInvoked_ThenReturnsResultFailed()
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Fail("Error retrieving record"));

        //Act
        var result = await _sut.HandleGetCancellationRequestAsync(Guid.NewGuid());

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetCancellationRequestAsyncInvoked_ThenReturnsResultFailed()
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        var result = await _sut.HandleGetCancellationRequestAsync(Guid.NewGuid());

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetCancellationRequestAsyncInvoked_ThenReturnsErrorOfTypeNotFound()
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        var result = await _sut.HandleGetCancellationRequestAsync(Guid.NewGuid());

        //Assert
        result.Errors.First().Should().BeOfType<PaymentExecutionError>();
        var errorType = ((PaymentExecutionError)result.Errors.First()).GetErrorType();
        errorType.Should().Be(ErrorType.PaymentTransactionNotFound);
    }

    [Theory, AutoData]
    public async Task GivenMapperThrowsException_WhenHandleGetCancellationRequestAsyncInvoked_ThenExceptionIsPropagated(
        PaymentTransactionDto mockedDto)
    {
        //Arrange
        var mockException = new Exception("oh dear!");
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok((PaymentTransactionDto?)mockedDto));
        _mockMapper.Setup(m => m.Map<CancellationRequest>(mockedDto))
            .Throws(mockException);

        //Act
        var act = async () => await _sut.HandleGetCancellationRequestAsync(Guid.NewGuid());

        //Assert
        await act.Should().ThrowAsync<Exception>();
    }
    #endregion

    #region HandleRequestCancellationAsync
    [Theory]
    [AutoData]
    public async Task GivenPaymentTransactionIsInCancelledState_WhenHandleRequestCancellationAsyncInvoked_ThenReturnsResultOk(CancellationRequest mockCancelRequest)
    {
        //Arrange
        _mockedCancellationValidationService.Setup(m => m.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(true);

        //Act
        var result = await _sut.HandleRequestCancellationAsync(
            mockCancelRequest, "reason", Guid.NewGuid(), Guid.NewGuid());

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task GivenPaymentTransactionStatusIsNotCancelledAndIsNotCancellable_WhenHandleRequestCancellationAsyncInvoked_ThenReturnsResultFail(CancellationRequest mockCancelRequest)
    {
        //Arrange
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(false);
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()))
            .Returns(false);

        //Act
        var result = await _sut.HandleRequestCancellationAsync(mockCancelRequest, "reason", Guid.NewGuid(), Guid.NewGuid());

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task GivenPaymentTransactionStatusIsNotCancelledAndIsNotCancellable_WhenHandleRequestCancellationAsyncInvoked_ThenReturnsErrorOfTypeNotCancellable(CancellationRequest mockCancelRequest)
    {
        //Arrange
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(false);
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()))
            .Returns(false);

        //Act
        var result = await _sut.HandleRequestCancellationAsync(mockCancelRequest, "reason", Guid.NewGuid(), Guid.NewGuid());

        //Assert
        result.Errors.First().Should().BeOfType<PaymentExecutionError>();
        var errorType = ((PaymentExecutionError)result.Errors.First()).GetErrorType();
        errorType.Should().Be(ErrorType.PaymentTransactionNotCancellable);
    }

    [Theory]
    [AutoData]
    public async Task GivenPaymentTransactionStatusIsCancellableAndNoSqsError_WhenHandleRequestCancellationAsyncInvoked_ThenReturnsResultOk(CancellationRequest mockCancelRequest)
    {
        //Arrange
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(false);
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()))
            .Returns(true);
        _mockCancelExecutionQueueService.Setup(m => m.SendMessageAsync(It.IsAny<PaymentCancellationRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        //Act
        var result = await _sut.HandleRequestCancellationAsync(mockCancelRequest,
            "reason", Guid.NewGuid(), Guid.NewGuid());

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task GivenCancellablePaymentTransaction_WhenHandleRequestCancellationAsyncInvoked_ThenCancellationServiceCalledWithExpectedPayload(CancellationRequest mockCancelRequest)
    {
        //Arrange
        var expectedCorrelationId = Guid.NewGuid();
        var expectedTenantId = Guid.NewGuid();
        var expectedDelaySeconds = 0;
        var expectedCancellationReason = "reason";

        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(false);
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()))
            .Returns(true);
        _mockCancelExecutionQueueService.Setup(m => m.SendMessageAsync(It.IsAny<PaymentCancellationRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        //Act
        await _sut.HandleRequestCancellationAsync(mockCancelRequest,
            expectedCancellationReason, expectedTenantId, expectedCorrelationId);

        //Assert
        _mockCancelExecutionQueueService.Verify(m => m.SendMessageAsync(It.Is<PaymentCancellationRequest>(payload =>
            payload.CancellationReason == expectedCancellationReason &&
            payload.ProviderType == mockCancelRequest.ProviderType.ToString() &&
            payload.PaymentRequestId == mockCancelRequest.PaymentRequestId
        ), expectedDelaySeconds, expectedCorrelationId.ToString(), expectedTenantId.ToString()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GivenSqsReturnsResultFail_WhenHandleRequestCancellationAsyncInvoked_ThenReturnsResultFail(CancellationRequest mockCancelRequest)
    {
        //Arrange
        var mockedFailMessage = "something has happened in sqs land";
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancelled(mockCancelRequest.Status))
            .Returns(false);
        _mockedCancellationValidationService.Setup(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()))
            .Returns(true);
        _mockCancelExecutionQueueService.Setup(m => m.SendMessageAsync(It.IsAny<PaymentCancellationRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Fail(mockedFailMessage));

        //Act
        var result = await _sut.HandleRequestCancellationAsync(mockCancelRequest,
            "reason", Guid.NewGuid(), Guid.NewGuid());

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(mockedFailMessage);
    }

    #endregion
}
