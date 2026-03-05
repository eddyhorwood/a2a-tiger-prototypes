using System.Net;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;
using PayReqPaymentRequestDto = PaymentExecution.PaymentRequestClient.Models.PaymentRequest;
using PayReqRequestStatusDto = PaymentExecution.PaymentRequestClient.Models.Enums.RequestStatus;

namespace PaymentExecution.Domain.UnitTests.Services;

public class ProcessCompleteMessageDomainServiceTests
{
    private readonly Mock<IPaymentTransactionRepository> _mockPaymentTransactionRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPaymentRequestClient> _mockPaymentRequestClient;
    private readonly ProcessCompleteMessageDomainService _sut;
    private readonly Mock<ILogger<ProcessCompleteMessageDomainService>> _mockLogger;

    public ProcessCompleteMessageDomainServiceTests()
    {
        _mockPaymentTransactionRepository = new Mock<IPaymentTransactionRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockPaymentRequestClient = new Mock<IPaymentRequestClient>();
        _mockLogger = new Mock<ILogger<ProcessCompleteMessageDomainService>>();

        _sut = new ProcessCompleteMessageDomainService(
            _mockPaymentTransactionRepository.Object,
            _mockMapper.Object,
            _mockPaymentRequestClient.Object,
            _mockLogger.Object);
    }

    #region EvaluateIfRecordIsInErrorState
    [Theory]
    [InlineAutoData(null, "6db0e021-ab08-4f93-8f81-56ed99d10385")]
    [InlineAutoData("tx_12345", null)]
    [InlineAutoData(null, null)]
    public void GivenPaymentProviderPaymentTransactionIdOrProviderServiceIdIsNull_WhenEvaluateIfRecordIsInErrorState_ThenReturnsResultFail(
        string? paymentProviderPaymentTransactionId,
        string? providerServiceId,
        PaymentTransactionDto paymentTransactionDto)
    {
        // Arrange
        paymentTransactionDto.PaymentProviderPaymentTransactionId = paymentProviderPaymentTransactionId;
        paymentTransactionDto.ProviderServiceId = Guid.TryParse(providerServiceId, out var providerServiceIdGuid)
            ? providerServiceIdGuid : null;

        // Act
        var result = _sut.EvaluateIfRecordIsInErrorState(paymentTransactionDto);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory, AutoData]
    public void GivenPaymentProviderPaymentTransactionIdAndProviderServiceIdAreNotNull_WhenEvaluateIfRecordIsInErrorState_ThenReturnsResultOk(
        PaymentTransactionDto paymentTransactionDto)
    {
        // Arrange
        paymentTransactionDto.PaymentProviderPaymentTransactionId = "valid-transaction-id";
        paymentTransactionDto.ProviderServiceId = Guid.NewGuid();

        // Act
        var result = _sut.EvaluateIfRecordIsInErrorState(paymentTransactionDto);

        // Assert
        Assert.True(result.IsSuccess);
    }
    #endregion

    #region ShouldEventBeIgnored
    [Theory]
    [InlineAutoData(TerminalStatus.Succeeded, "Failed")]
    [InlineAutoData(TerminalStatus.Failed, "Succeeded")]
    [InlineAutoData(TerminalStatus.Cancelled, "Failed")]
    public void GivenDbStatusIsTerminalAndDiffersFromIncomingMessageStatus_WhenShouldEventBeIgnored_ThenReturnsTrue(
        TerminalStatus dtoStatus,
        string incomingMessageStatus,
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        paymentTransactionDto.Status = dtoStatus.ToString();
        messageBeingProcessed.Status = incomingMessageStatus;

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineAutoData("in_progress", "in_progress")]
    [InlineAutoData("in_progress", "Succeeded")]
    public void GivenDbStateIsNotTerminalAndIncomingMessageIsMoreRecent_WhenShouldEventBeIgnored_ThenReturnsFalse(
        string dtoStatus,
        string incomingMessageStatus,
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        paymentTransactionDto.Status = dtoStatus;
        messageBeingProcessed.Status = incomingMessageStatus;

        paymentTransactionDto.EventCreatedDateTimeUtc = DateTime.UtcNow;
        messageBeingProcessed.EventCreatedDateTime = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineAutoData("Succeeded", "Succeeded")]
    [InlineAutoData("Failed", "Failed")]
    [InlineAutoData("Cancelled", "Cancelled")]
    public void GivenDuplicateOfProcessedMessage_WhenShouldEventBeIgnored_ThenReturnsFalse(
        string dtoStatus,
        string incomingMessageStatus,
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        paymentTransactionDto.Status = dtoStatus;
        messageBeingProcessed.Status = incomingMessageStatus;

        var eventDateTime = DateTime.UtcNow;
        paymentTransactionDto.EventCreatedDateTimeUtc = eventDateTime;
        messageBeingProcessed.EventCreatedDateTime = eventDateTime;

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoData]
    public void GivenIncomingMessageEventDateTimeIsMoreRecent_WhenShouldEventBeIgnored_ThenReturnsFalse(
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        messageBeingProcessed.EventCreatedDateTime = DateTime.UtcNow.AddMinutes(5);

        paymentTransactionDto.EventCreatedDateTimeUtc = DateTime.UtcNow;
        paymentTransactionDto.Status = "in_progress";
        messageBeingProcessed.Status = "Succeeded";

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.False(result);
    }

    [Theory, AutoData]
    public void GivenDbRecordEventDateTimeIsMoreRecent_WhenShouldEventBeIgnored_ThenReturnsTrue(
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        paymentTransactionDto.EventCreatedDateTimeUtc = DateTime.UtcNow.AddMinutes(5);

        messageBeingProcessed.EventCreatedDateTime = DateTime.UtcNow;
        paymentTransactionDto.Status = "in_progress";
        messageBeingProcessed.Status = "Succeeded";

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.True(result);
    }

    [Theory, AutoData]
    public void GivenDbEventCreatedDateTimeIsNull_WhenShouldEventBeIgnored_ThenReturnsFalse(
        PaymentTransactionDto paymentTransactionDto,
        CompleteMessage messageBeingProcessed)
    {
        // Arrange
        paymentTransactionDto.EventCreatedDateTimeUtc = null;

        paymentTransactionDto.Status = "in_progress";
        messageBeingProcessed.Status = "Succeeded";

        // Act
        var result = _sut.ShouldEventBeIgnored(paymentTransactionDto, messageBeingProcessed);

        // Assert
        Assert.False(result);
    }
    #endregion

    #region HandleUpdateDbAsync
    [Theory]
    [AutoData]
    public async Task GivenSucceededStatusAndDbUpdateSuccessful_WhenHandleUpdateDbAsync_ThenReturnsResultOk(
        CompleteMessage domainMessage,
        UpdateSuccessPaymentTransactionDto mockedUpdateSuccessStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Succeeded;

        _mockMapper.Setup(m => m.Map<UpdateSuccessPaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateSuccessStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateSuccessPaymentTransactionData(mockedUpdateSuccessStatusDto))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [AutoData]
    public async Task GivenSucceededStatusAndDbUpdateFails_WhenHandleUpdateDbAsync_ThenReturnsResultFail(
        CompleteMessage domainMessage,
        UpdateSuccessPaymentTransactionDto mockedUpdateSuccessStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Succeeded;

        _mockMapper.Setup(m => m.Map<UpdateSuccessPaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateSuccessStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateSuccessPaymentTransactionData(mockedUpdateSuccessStatusDto))
            .ReturnsAsync(Result.Fail("Something happened!"));

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory]
    [AutoData]
    public async Task GivenFailedStatusAndDbUpdateSuccessful_WhenHandleUpdateDbAsync_ThenReturnsResultOk(
        CompleteMessage domainMessage,
        UpdateFailurePaymentTransactionDto mockedUpdateFailureStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Failed;

        _mockMapper.Setup(m => m.Map<UpdateFailurePaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateFailureStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateFailurePaymentTransactionData(mockedUpdateFailureStatusDto))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [AutoData]
    public async Task GivenFailedStatusAndDbUpdateFails_WhenHandleUpdateDbAsync_ThenReturnsResultFail(
        CompleteMessage domainMessage,
        UpdateFailurePaymentTransactionDto mockedUpdateFailureStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Failed;

        _mockMapper.Setup(m => m.Map<UpdateFailurePaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateFailureStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateFailurePaymentTransactionData(mockedUpdateFailureStatusDto))
            .ReturnsAsync(Result.Fail("Something happened!"));

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory]
    [AutoData]
    public async Task GivenCancelledStatusAndDbUpdateSuccessful_WhenHandleUpdateDbAsync_ThenReturnsResultOk(
        CompleteMessage domainMessage,
        UpdateCancelledPaymentTransactionDto mockedUpdateCancelledStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Cancelled;

        _mockMapper.Setup(m => m.Map<UpdateCancelledPaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateCancelledStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateCancelledPaymentTransactionData(mockedUpdateCancelledStatusDto))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsSuccess);
        _mockPaymentTransactionRepository.Verify(repo => repo.UpdateCancelledPaymentTransactionData(mockedUpdateCancelledStatusDto), Times.Once);
        _mockMapper.Verify(mapper => mapper.Map<UpdateCancelledPaymentTransactionDto>(domainMessage), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GivenCancelledStatusAndDbUpdateFails_WhenHandleUpdateDbAsync_ThenReturnsResultFail(
        CompleteMessage domainMessage,
        UpdateCancelledPaymentTransactionDto mockedUpdateCancelledStatusDto)
    {
        // Arrange
        var mockedStatus = StripeValidCompleteStatus.Cancelled;

        _mockMapper.Setup(m => m.Map<UpdateCancelledPaymentTransactionDto>(domainMessage))
            .Returns(mockedUpdateCancelledStatusDto);
        _mockPaymentTransactionRepository.Setup(r => r.UpdateCancelledPaymentTransactionData(mockedUpdateCancelledStatusDto))
            .ReturnsAsync(Result.Fail("Something happened!"));

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, mockedStatus);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Theory]
    [AutoData]
    public async Task GivenUnexpectedStatus_WhenHandleUpdateDbAsync_ThenReturnsResultFailAndLogs(
        CompleteMessage domainMessage)
    {
        // Arrange
        var unexpectedStatus = (StripeValidCompleteStatus)999;
        var errorLogPrefix = "Received unexpected status";

        // Act
        var result = await _sut.HandleUpdateDbAsync(domainMessage, unexpectedStatus);

        // Assert
        Assert.True(result.IsFailed);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Error, errorLogPrefix, 1);
    }

    #endregion

    #region HandleExecutionSucceedPaymentRequestAsync
    [Theory, AutoData]
    public async Task
        GivenCallToExecutionSucceedPaymentRequestSuccessful_WhenHandleExecutionSucceedPaymentRequestAsync_ThenReturnsResultOk(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Ok());

        //Act
        var result = await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsSuccess);
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.NotFound)]
    [InlineAutoData(HttpStatusCode.Unauthorized)]
    public async Task
        GivenCallToExecutionSuccessReturnsNonSuccessStatusCodeExcludingBadRequest_WhenHandleExecutionSucceedPaymentRequestAsync_ThenReturnsResultFail(
            HttpStatusCode mockedStatusCode,
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        var mockedError = new PaymentExecutionError("Failed to succeed payment request.", mockedStatusCode);
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(mockedError));

        //Act
        var result = await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        result.IsFailed.Should().BeTrue();
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenExeSuccessReturnsBadRequestAndPrStatusIsSuccess_WhenHandleExecutionSucceedPaymentRequestAsync_ThenReturnsResultOk(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest,
            PayReqPaymentRequestDto mockPayReqPaymentRequestDto,
            PaymentRequest mockDomainPaymentRequest)
    {
        //arrange
        mockPayReqPaymentRequestDto.Status = PayReqRequestStatusDto.success;
        mockDomainPaymentRequest.Status = RequestStatus.success;
        var mockedError = new PaymentExecutionError("Failed to succeed payment request.", HttpStatusCode.BadRequest);
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockMapper.Setup(m => m.Map<PaymentRequest>(mockPayReqPaymentRequestDto))
            .Returns(mockDomainPaymentRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(mockedError));
        _mockPaymentRequestClient.Setup(r => r.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(mockPayReqPaymentRequestDto));

        //Act
        var result = await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        result.IsSuccess.Should().BeTrue();
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
        _mockPaymentRequestClient.Verify(m => m.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineAutoData(RequestStatus.created)]
    [InlineAutoData(RequestStatus.scheduled)]
    [InlineAutoData(RequestStatus.awaitingexecution)]
    [InlineAutoData(RequestStatus.cancelled)]
    [InlineAutoData(RequestStatus.failed)]
    public async Task
        GivenExeSuccessReturnsBadRequestAndPrStatusIsNotSuccess_WhenHandleExecutionSucceedPaymentRequestAsync_ThenReturnsResultFail(
            RequestStatus mockRequestStatus,
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest,
            PayReqPaymentRequestDto mockPayReqPaymentRequestDto,
            PaymentRequest mockDomainPaymentRequest)
    {
        //arrange
        mockDomainPaymentRequest.Status = mockRequestStatus;
        var mockedError = new PaymentExecutionError("Failed to succeed payment request.", HttpStatusCode.BadRequest);
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockMapper.Setup(m => m.Map<PaymentRequest>(mockPayReqPaymentRequestDto))
            .Returns(mockDomainPaymentRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(mockedError));
        _mockPaymentRequestClient.Setup(r => r.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(mockPayReqPaymentRequestDto));

        //Act
        var result = await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        result.IsFailed.Should().BeTrue();
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
        _mockPaymentRequestClient.Verify(m => m.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenExeSuccessReturnsBadRequestAndMappingToDomainPaymentRequestThrowsAnException_WhenHandleExecutionSucceedPaymentRequestAsync_ThenPropagatesException(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest,
            PayReqPaymentRequestDto mockPayReqPaymentRequestDto)
    {
        //arrange
        var mockedException = new Exception("oh dear!");
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(new PaymentExecutionError("Failed to succeed payment request.", HttpStatusCode.BadRequest)))
            .Verifiable();
        _mockPaymentRequestClient.Setup(r => r.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(mockPayReqPaymentRequestDto))
            .Verifiable();
        _mockMapper.Setup(m => m.Map<PaymentRequest>(mockPayReqPaymentRequestDto))
            .Throws(mockedException)
            .Verifiable();

        //Act
        var act = async () => await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("oh dear!");
        _mockPaymentRequestClient.Verify();
        _mockPaymentRequestClient.Verify();
        _mockMapper.Verify();
    }

    [Theory, AutoData]
    public async Task
        GivenExeSuccessReturnsBadRequestWithANonPaymentRequestError_WhenHandleExecutionSucceedPaymentRequestAsync_ThenThrowsException(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail("This is not a payment execution error"));

        //Act
        var act = async () => await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error is not of type PaymentExecutionError with HttpStatusCode");
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenExeSuccessReturnsBadRequestWithPaymentRequestErrorWithoutStatusCode_WhenHandleExecutionSucceedPaymentRequestAsync_ThenThrowsException(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        var mockedError = new PaymentExecutionError("Execution error without setting status code");
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(mockedError));

        //Act
        var act = async () => await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error is not of type PaymentExecutionError with HttpStatusCode");
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenExeSuccessReturnsBadRequestAndGetPaymentRequestFails_WhenHandleExecutionSucceedPaymentRequestAsync_ThenReturnsResultFail(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        var mockedError = new PaymentExecutionError("Failed to succeed payment request.", HttpStatusCode.BadRequest);
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail(mockedError));
        _mockPaymentRequestClient.Setup(r => r.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ReturnsAsync(Result.Fail<PayReqPaymentRequestDto>("oopsie!"));

        //Act
        var result = await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsFailed);
        _mockPaymentRequestClient.Verify(m => m.ExecutionSucceedPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<SuccessPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
        _mockPaymentRequestClient.Verify(m => m.GetPaymentRequestByPaymentRequestId(It.IsAny<Guid>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenPaymentRequestClientThrowsException_WhenHandleExecutionSucceedPaymentRequestAsync_ThenExceptionIsPropagated(
            CompleteMessage domainMessage,
            SuccessPaymentRequest mockedSuccessRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Returns(mockedSuccessRequest);
        _mockPaymentRequestClient.Setup(r => r.ExecutionSucceedPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedSuccessRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ThrowsAsync(new Exception("oh dear something went wrong!"));

        //Act
        var act = async () => await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);
    }

    [Theory, AutoData]
    public async Task
        GivenMapperThrowsException_WhenHandleExecutionSucceedPaymentRequestAsync_ThenExceptionIsPropagated(CompleteMessage domainMessage)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<SuccessPaymentRequest>(domainMessage))
            .Throws(new Exception("oh dear mapping exception!"));

        //Act
        var act = async () => await _sut.HandleExecutionSucceedPaymentRequestAsync(domainMessage);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);
    }
    #endregion



    #region HandleFailPaymentRequestAsync
    [Theory, AutoData]
    public async Task
        GivenCallToPaymentRequestSuccessful_WhenHandleFailPaymentRequestAsync_ThenReturnsResultOk(
            CompleteMessage domainMessage,
            FailurePaymentRequest mockedFailureRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<FailurePaymentRequest>(domainMessage))
            .Returns(mockedFailureRequest);
        _mockPaymentRequestClient.Setup(r => r.FailPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedFailureRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Ok);

        //Act
        var result = await _sut.HandleFailPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsSuccess);
        _mockPaymentRequestClient.Verify(m => m.FailPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<FailurePaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }


    [Theory, AutoData]
    public async Task
        GivenCallToPaymentRequestUnsuccessful_WhenHandleFailPaymentRequestAsync_ThenReturnsResultFail(
            CompleteMessage domainMessage,
            FailurePaymentRequest mockedFailureRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<FailurePaymentRequest>(domainMessage))
            .Returns(mockedFailureRequest);
        _mockPaymentRequestClient.Setup(r => r.FailPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedFailureRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail("something went wrong!"));

        //Act
        var result = await _sut.HandleFailPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsFailed);
        _mockPaymentRequestClient.Verify(m => m.FailPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<FailurePaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }


    [Theory, AutoData]
    public async Task
        GivenPaymentRequestClientThrowsException_WhenHandleFailPaymentRequestAsync_ThenExceptionIsPropagated(
            CompleteMessage domainMessage,
            FailurePaymentRequest mockedFailureRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<FailurePaymentRequest>(domainMessage))
            .Returns(mockedFailureRequest);
        _mockPaymentRequestClient.Setup(r => r.FailPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedFailureRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ThrowsAsync(new Exception("oh dear something went wrong!"));

        //Act
        var act = async () => await _sut.HandleFailPaymentRequestAsync(domainMessage);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);
    }

    [Theory, AutoData]
    public async Task
        GivenMapperThrowsException_WhenHandleFailPaymentRequestAsync_ThenExceptionIsPropagated(CompleteMessage domainMessage)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<FailurePaymentRequest>(domainMessage))
            .Throws(new Exception("oh dear mapping exception!"));

        //Act
        var act = async () => await _sut.HandleFailPaymentRequestAsync(domainMessage);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);
    }
    #endregion

    #region HandleCancelPaymentRequestAsync
    [Theory, AutoData]
    public async Task
        GivenCallToPaymentRequestSuccessful_WhenHandleCancelPaymentRequestAsync_ThenReturnsResultOk(
            CompleteMessage domainMessage,
            PaymentRequestClient.Models.Requests.CancelPaymentRequest mockedCancelPaymentRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(domainMessage))
            .Returns(mockedCancelPaymentRequest);
        _mockPaymentRequestClient.Setup(r => r.CancelPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedCancelPaymentRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Ok);

        //Act
        var result = await _sut.HandleCancelPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsSuccess);
        _mockPaymentRequestClient.Verify(m => m.CancelPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }


    [Theory, AutoData]
    public async Task
        GivenCallToPaymentRequestUnsuccessful_WhenHandleCancelPaymentRequestAsync_ThenReturnsResultFail(
            CompleteMessage domainMessage,
            PaymentRequestClient.Models.Requests.CancelPaymentRequest mockedCancelPaymentRequest)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(domainMessage))
            .Returns(mockedCancelPaymentRequest);
        _mockPaymentRequestClient.Setup(r => r.CancelPaymentRequest(
                domainMessage.PaymentRequestId,
                mockedCancelPaymentRequest,
                domainMessage.XeroCorrelationId,
                domainMessage.XeroTenantId))
            .ReturnsAsync(Result.Fail("something went wrong!"));

        //Act
        var result = await _sut.HandleCancelPaymentRequestAsync(domainMessage);

        //Assert
        Assert.True(result.IsFailed);
        _mockPaymentRequestClient.Verify(m => m.CancelPaymentRequest(It.IsAny<Guid>(),
            It.IsAny<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task
        GivenMapperThrowsException_WhenHandleCancelPaymentRequestAsync_ThenExceptionIsPropagated(CompleteMessage domainMessage)
    {
        //arrange
        _mockMapper.Setup(m => m.Map<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(domainMessage))
            .Throws(new Exception("oh dear mapping exception!"));

        //Act
        var act = async () => await _sut.HandleCancelPaymentRequestAsync(domainMessage);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);
    }
    #endregion
}
