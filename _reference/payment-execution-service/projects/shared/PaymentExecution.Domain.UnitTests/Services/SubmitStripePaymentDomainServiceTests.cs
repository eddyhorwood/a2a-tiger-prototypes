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
using PaymentExecution.SqsIntegrationClient.Service;
using PaymentExecution.StripeExecutionClient.Contracts;
using PaymentExecution.TestUtilities;
using PayReqPaymentRequestDto = PaymentExecution.PaymentRequestClient.Models.PaymentRequest;
using StripeExeSubmitPaymentRequestDto = PaymentExecution.StripeExecutionClient.Contracts.Models.StripeExeSubmitPaymentRequestDto;
using StripeExeSubmitPaymentResponseDto = PaymentExecution.StripeExecutionClient.Contracts.Models.StripeExeSubmitPaymentResponseDto;

namespace PaymentExecution.Domain.UnitTests.Services;

public class SubmitStripePaymentDomainServiceTests
{
    private readonly Mock<IPaymentRequestClient> _paymentRequestClientMock = new();
    private readonly Mock<IPaymentTransactionRepository> _paymentTransactionRepositoryMock = new();
    private readonly Mock<IStripeExecutionClient> _stripeExecutionClientMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ICancelExecutionQueueService> _cancelExecutionQueueServiceMock = new();
    private readonly SubmitStripePaymentDomainService _domainService;
    private readonly Mock<ILogger<SubmitStripePaymentDomainService>> _logger = new();

    public SubmitStripePaymentDomainServiceTests()
    {
        _domainService = new SubmitStripePaymentDomainService(
            _paymentRequestClientMock.Object,
            _paymentTransactionRepositoryMock.Object,
            _stripeExecutionClientMock.Object,
            _logger.Object,
            _mapperMock.Object,
            _cancelExecutionQueueServiceMock.Object);
    }

    [Fact]
    public async Task GivenPaymentRequestFails_WhenSubmitToPaymentRequest_ThenReturnsExpectedError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var expectedErrorMessage = "some-payment-request-error";
        _paymentRequestClientMock
            .Setup(client => client.SubmitPaymentRequest(paymentRequestId))
            .ReturnsAsync(Result.Fail(new PaymentExecutionError(expectedErrorMessage)));
        var expectedError = new PaymentExecutionError(
            expectedErrorMessage,
            ErrorType.BadPaymentRequest,
            ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed);

        // Act
        var result = await _domainService.SubmitToPaymentRequestAsync(paymentRequestId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equivalent(expectedError, result.Errors.FirstOrDefault());
    }

    [Theory, AutoData]
    public async Task GivenPaymentRequestSuccessAndMapToDomainPaymentRequestThrowsException_WhenSubmitToPaymentRequest_ThenPropagatesException(
        PayReqPaymentRequestDto paymentRequest)
    {
        // Arrange
        var paymentRequestId = paymentRequest.PaymentRequestId;

        _paymentRequestClientMock
            .Setup(client => client.SubmitPaymentRequest(paymentRequestId))
            .ReturnsAsync(Result.Ok(paymentRequest));

        _mapperMock
            .Setup(m => m.Map<PaymentRequest>(paymentRequest))
            .Throws(new Exception("something has happened mapping!!"));

        // Act
        var act = async () => await _domainService.SubmitToPaymentRequestAsync(paymentRequestId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("something has happened mapping!!");
    }

    [AutoData]
    [Theory]
    public async Task GivenPaymentRequestSuccess_WhenSubmitToPaymentRequest_ThenReturnSuccess(
        PayReqPaymentRequestDto paymentRequest,
        PaymentRequest domainPaymentRequest)
    {
        // Arrange
        var paymentRequestId = paymentRequest.PaymentRequestId;

        _paymentRequestClientMock
            .Setup(client => client.SubmitPaymentRequest(paymentRequestId))
            .ReturnsAsync(Result.Ok(paymentRequest));

        _mapperMock
            .Setup(m => m.Map<PaymentRequest>(paymentRequest))
            .Returns(domainPaymentRequest);

        // Act
        var result = await _domainService.SubmitToPaymentRequestAsync(paymentRequestId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(domainPaymentRequest, result.Value);
    }

    [AutoData]
    [Theory]
    public async Task GivenPaymentRequestClientThrowsException_WhenSubmitToPaymentRequest_ThenExceptionPropagated(
        PayReqPaymentRequestDto paymentRequest)
    {
        // Arrange
        var paymentRequestId = paymentRequest.PaymentRequestId;

        _paymentRequestClientMock
            .Setup(client => client.SubmitPaymentRequest(paymentRequestId))
            .ThrowsAsync(new Exception("something has happened!"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _domainService.SubmitToPaymentRequestAsync(paymentRequestId));
    }

    [Fact]
    public async Task
        GivenInsertFails_WhenCreatePaymentTransactionWithCompensationAction_ThenReturnFailedResultAndMarkTransactionFailedAsCompensationAction()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _paymentTransactionRepositoryMock
            .Setup(repo => repo.InsertPaymentTransactionIfNotExist(It.IsAny<InsertPaymentTransactionDto>()))
            .ReturnsAsync(Result.Fail(new PaymentExecutionError("Insert failed")));

        // Act
        var result =
            await _domainService.CreatePaymentTransactionWithCompensationActionAsync(paymentRequestId, organisationId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Insert failed", result.GetFirstErrorMessage());
        _paymentRequestClientMock
            .Verify(client => client.FailPaymentRequest(paymentRequestId, It.IsAny<FailurePaymentRequest>()),
                Times.Once);
    }

    [Fact]
    public async Task GivenInsertSucceeds_WhenCreatePaymentTransactionWithCompensationAction_ThenReturnSuccess()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _paymentTransactionRepositoryMock
            .Setup(repo => repo.InsertPaymentTransactionIfNotExist(It.IsAny<InsertPaymentTransactionDto>()))
            .ReturnsAsync(Result.Ok(transactionId));

        // Act
        var result =
            await _domainService.CreatePaymentTransactionWithCompensationActionAsync(paymentRequestId, organisationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(transactionId, result.Value);
    }

    [AutoData]
    [Theory]
    public async Task
        GivenStripeExecutionFails_WhenSubmitRequestToStripeExecutionWithCompensationAction_ThenReturnFailedResultAndMarkTransactionAndPaymentRequestFailedAsCompensationAction(
            PaymentRequest paymentRequest)
    {
        // Arrange
        var paymentMethods = new List<string> { "card" };
        var paymentMethodId = "method_123";
        var failedStatus = nameof(TransactionStatus.Failed);
        _stripeExecutionClientMock
            .Setup(service => service.SubmitPaymentAsync(It.IsAny<StripeExeSubmitPaymentRequestDto>()))
            .ReturnsAsync(Result.Fail(new PaymentExecutionError("Stripe execution failed")));

        _paymentTransactionRepositoryMock.Setup(repo =>
                repo.SetPaymentTransactionFailed(It.IsAny<Guid>(), It.IsAny<string>(), failedStatus))
            .ReturnsAsync(Result.Ok());

        _paymentRequestClientMock
            .Setup(client =>
                client.FailPaymentRequest(paymentRequest.PaymentRequestId, It.IsAny<FailurePaymentRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result =
            await _domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
                paymentMethods, paymentMethodId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Stripe execution failed", result.GetFirstErrorMessage());
        _paymentRequestClientMock
            .Verify(
                client => client.FailPaymentRequest(paymentRequest.PaymentRequestId, It.IsAny<FailurePaymentRequest>()),
                Times.Once);
        _paymentTransactionRepositoryMock.Verify(repo =>
            repo.SetPaymentTransactionFailed(paymentRequest.PaymentRequestId, It.IsAny<string>(),
                failedStatus), Times.Once);
    }

    [AutoData]
    [Theory]
    public async Task
        GivenStripeExecutionSucceeds_WhenSubmitRequestToStripeExecutionWithCompensationAction_ThenReturnSuccessWithSubmitPaymentResponse(
            PaymentRequest paymentRequest)
    {
        // Arrange
        var paymentMethods = new List<string> { "card" };
        var paymentMethodId = "method_123";
        var providerServiceId = Guid.NewGuid();
        var stripeExeResponse = new StripeExeSubmitPaymentResponseDto
        {
            PaymentIntentId = "pi_123",
            ClientSecret = "secret_123",
            ProviderServiceId = providerServiceId
        };
        var expectedMappedDomainResponse = new SubmittedPayment()
        {
            PaymentIntentId = "pi_123",
            ClientSecret = "secret_123",
            ProviderServiceId = providerServiceId
        };
        _stripeExecutionClientMock
            .Setup(service => service.SubmitPaymentAsync(It.IsAny<StripeExeSubmitPaymentRequestDto>()))
            .ReturnsAsync(Result.Ok(stripeExeResponse));
        _mapperMock
            .Setup(m => m.Map<SubmittedPayment>(stripeExeResponse))
            .Returns(expectedMappedDomainResponse);

        // Act
        var result =
            await _domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
                paymentMethods, paymentMethodId);

        // Assert
        Assert.True(result.IsSuccess);
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(expectedMappedDomainResponse);
    }

    [AutoData]
    [Theory]
    public async Task
        GivenStripeExecutionFailureWithProviderCode_WhenSubmitRequestToStripeExecutionWithCompensationAction_ThenExpectedCompensationOccursWithFailureDetailsPrefixedWithProviderCode(
            PaymentRequest paymentRequest)
    {
        // Arrange
        var mockFailureMessage = "something has happened!";
        var mockProviderErrorCode = "invalid_account";
        var mockError = new PaymentExecutionError(mockFailureMessage, mockProviderErrorCode, HttpStatusCode.BadRequest);
        var expectedFailureDetails = string.Concat(mockProviderErrorCode, "-", mockFailureMessage);
        var failedStatus = nameof(TransactionStatus.Failed);
        _stripeExecutionClientMock
            .Setup(service => service.SubmitPaymentAsync(It.IsAny<StripeExeSubmitPaymentRequestDto>()))
            .ReturnsAsync(Result.Fail(mockError));
        _paymentTransactionRepositoryMock
            .Setup(m => m.SetPaymentTransactionFailed(It.IsAny<Guid>(), It.IsAny<string>(), failedStatus))
            .ReturnsAsync(Result.Ok());
        _paymentRequestClientMock.Setup(m => m.FailPaymentRequest(It.IsAny<Guid>(), It.IsAny<FailurePaymentRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
            ["card"], "method_123");

        // Assert
        _paymentTransactionRepositoryMock.Verify(
            m => m.SetPaymentTransactionFailed(It.IsAny<Guid>(), expectedFailureDetails, failedStatus), Times.Once);
        _paymentRequestClientMock.Verify(m => m.FailPaymentRequest(It.IsAny<Guid>(),
            It.Is<FailurePaymentRequest>(req => req.FailureDetails == expectedFailureDetails)), Times.Once);
    }

    [AutoData]
    [Theory]
    public async Task
        GivenStripeExecutionFailureWithNoProviderCode_WhenSubmitRequestToStripeExecutionWithCompensationAction_ThenExpectedCompensationOccursWithFailureDetailsAsFailureMessage(
            PaymentRequest paymentRequest)
    {
        // Arrange
        var mockFailureMessage = "something has happened!";
        var mockError = new PaymentExecutionError(mockFailureMessage, null, HttpStatusCode.PaymentRequired);
        var expectedFailureDetails = mockFailureMessage;
        var failedStatus = nameof(TransactionStatus.Failed);
        _stripeExecutionClientMock
            .Setup(service => service.SubmitPaymentAsync(It.IsAny<StripeExeSubmitPaymentRequestDto>()))
            .ReturnsAsync(Result.Fail(mockError));
        _paymentTransactionRepositoryMock
            .Setup(m => m.SetPaymentTransactionFailed(It.IsAny<Guid>(), It.IsAny<string>(), failedStatus))
            .ReturnsAsync(Result.Ok());
        _paymentRequestClientMock.Setup(m => m.FailPaymentRequest(It.IsAny<Guid>(), It.IsAny<FailurePaymentRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        await _domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
            ["card"], "method_123");

        // Assert
        _paymentTransactionRepositoryMock.Verify(
            m => m.SetPaymentTransactionFailed(It.IsAny<Guid>(), expectedFailureDetails, failedStatus), Times.Once);
        _paymentRequestClientMock.Verify(m => m.FailPaymentRequest(It.IsAny<Guid>(),
            It.Is<FailurePaymentRequest>(req => req.FailureDetails == expectedFailureDetails)), Times.Once);
    }

    [AutoData]
    [Theory]
    public async Task
        GivenStripeExecutionIntegrationFailure_WhenSubmitRequestToStripeExecutionWithCompensationAction_ThenReturnsExpectedError(
            PaymentRequest paymentRequest)
    {
        // Arrange
        var mockFailureMessage = "something has happened!";
        var mockProviderErrorCode = "invalid_account";
        var mockStripeExecutionError =
            new PaymentExecutionError(mockFailureMessage, mockProviderErrorCode, HttpStatusCode.BadRequest);
        var expectedError = new PaymentExecutionError(
            mockFailureMessage,
            ErrorType.PaymentFailed,
            ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed,
            mockProviderErrorCode);
        expectedError.Metadata.Add("HttpStatusCode", HttpStatusCode.BadRequest);
        var failedStatus = nameof(TransactionStatus.Failed);

        _stripeExecutionClientMock
            .Setup(service => service.SubmitPaymentAsync(It.IsAny<StripeExeSubmitPaymentRequestDto>()))
            .ReturnsAsync(Result.Fail(mockStripeExecutionError));
        _paymentTransactionRepositoryMock
            .Setup(m => m.SetPaymentTransactionFailed(It.IsAny<Guid>(), It.IsAny<string>(), failedStatus))
            .ReturnsAsync(Result.Ok());
        _paymentRequestClientMock.Setup(m => m.FailPaymentRequest(It.IsAny<Guid>(), It.IsAny<FailurePaymentRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
            ["card"], "method_123");

        // Assert
        result.Errors.FirstOrDefault().Should().NotBeNull();
        result.Errors[0].Should().BeOfType<PaymentExecutionError>()
            .Which.Should().BeEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GivenUpdateFails_WhenUpdatePaymentTransactionWithProviderDetails_ThenLogsError()
    {
        // Arrange
        var mockRepositoryErrorMessage = "Something has happened";
        var providerPaymentTransactionId = "pi_123";
        var mockPaymentRequestId = Guid.NewGuid();
        var paymentTransactionId = Guid.NewGuid();
        var providerServiceId = Guid.NewGuid();

        var expectedLogPrefix = $"Failed to update payment transaction with provider details";
        _paymentTransactionRepositoryMock
            .Setup(repo => repo.UpdatePaymentTransactionWithProviderDetails(It.IsAny<UpdateForSubmitFlowDto>()))
            .ReturnsAsync(Result.Fail(mockRepositoryErrorMessage));

        // Act
        await _domainService.TryToUpdatePaymentTransactionWithProviderDetailsAsync(providerPaymentTransactionId,
            paymentTransactionId,
            providerServiceId, mockPaymentRequestId);

        // Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Error, expectedLogPrefix, 1);
    }

    [Fact]
    public async Task GivenUpdateSucceeds_WhenUpdatePaymentTransactionWithProviderDetails_ThenReturnSuccess()
    {
        // Arrange
        var paymentIntentId = "pi_123";
        var paymentTransactionId = Guid.NewGuid();
        var providerServiceId = Guid.NewGuid();
        _paymentTransactionRepositoryMock
            .Setup(repo => repo.UpdatePaymentTransactionWithProviderDetails(It.IsAny<UpdateForSubmitFlowDto>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _domainService.TryToUpdatePaymentTransactionWithProviderDetailsAsync(paymentIntentId,
            paymentTransactionId,
            providerServiceId, Guid.NewGuid());

        // Assert
        _logger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task
        GivenValidRequest_WhenTryToSendMessageToCancelExecutionQueueAsync_ThenSendsMessageWithCorrectParameters()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var ttlInSeconds = 300;
        var xeroCorrelationId = Guid.NewGuid().ToString();
        var xeroTenantId = Guid.NewGuid().ToString();

        _cancelExecutionQueueServiceMock
            .Setup(svc => svc.SendMessageAsync(It.IsAny<PaymentCancellationRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _domainService.TryToSendMessageToCancelExecutionQueueAsync(
            paymentRequestId, ttlInSeconds, xeroCorrelationId, xeroTenantId);

        // Assert
        _cancelExecutionQueueServiceMock.Verify(
            svc => svc.SendMessageAsync(
                It.Is<PaymentCancellationRequest>(msg =>
                    msg.PaymentRequestId == paymentRequestId &&
                    msg.ProviderType == nameof(ProviderType.Stripe) &&
                    msg.CancellationReason == nameof(CancellationReason.Abandoned)),
                ttlInSeconds,
                xeroCorrelationId,
                xeroTenantId),
            Times.Once);
        _logger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task
        GivenSendMessageFails_WhenTryToSendMessageToCancelExecutionQueueAsync_ThenLogsWarningButDoesNotThrow()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var ttlInSeconds = 300;
        var xeroCorrelationId = Guid.NewGuid().ToString();
        var xeroTenantId = Guid.NewGuid().ToString();
        var errorMessage = "Failed to send to queue";

        _cancelExecutionQueueServiceMock
            .Setup(svc => svc.SendMessageAsync(It.IsAny<PaymentCancellationRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Fail(errorMessage));

        // Act - Should not throw despite SendMessage failure
        await _domainService.TryToSendMessageToCancelExecutionQueueAsync(
            paymentRequestId, ttlInSeconds, xeroCorrelationId, xeroTenantId);

        // Assert - Warning was logged but no exception thrown
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(
            _logger,
            LogLevel.Warning,
            "Failed to send message to cancel execution queue",
            1);
    }
}
