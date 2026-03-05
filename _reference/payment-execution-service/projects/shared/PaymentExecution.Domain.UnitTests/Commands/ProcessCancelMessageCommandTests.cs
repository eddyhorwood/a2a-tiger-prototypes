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
using PaymentExecution.FeatureFlagClient;

namespace PaymentExecution.Domain.UnitTests.Commands;

public class ProcessCancelMessageCommandTests
{
    private readonly Mock<ILogger<ProcessCancelMessageCommandHandler>> _mockLogger;
    private readonly Mock<ICancelDomainService> _mockCancelDomainService;
    private readonly Mock<IGetProviderStateDomainService> _mockGetProviderStateDomainService;
    private readonly Mock<ICancellationValidationService> _mockValidationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IFeatureFlagClient> _mockFeatureFlagClient;
    private readonly ProcessCancelMessageCommandHandler _handler;
    private const string Stripe = "Stripe";

    public ProcessCancelMessageCommandTests()
    {
        _mockLogger = new Mock<ILogger<ProcessCancelMessageCommandHandler>>();
        _mockCancelDomainService = new Mock<ICancelDomainService>();
        _mockGetProviderStateDomainService = new Mock<IGetProviderStateDomainService>();
        _mockValidationService = new Mock<ICancellationValidationService>();
        _mockFeatureFlagClient = new Mock<IFeatureFlagClient>();
        _mockMapper = new Mock<IMapper>();

        _handler = new ProcessCancelMessageCommandHandler(
            _mockLogger.Object,
            _mockGetProviderStateDomainService.Object,
            _mockCancelDomainService.Object,
            _mockValidationService.Object,
            _mockFeatureFlagClient.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GivenPaymentTransactionNotFound_WhenHandlerCalled_ThenReturnsPaymentTransactionNotFoundError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var command = CreateCommand(paymentRequestId, Stripe);
        var expectedError = new PaymentExecutionError(
            "Payment transaction not found", ErrorType.PaymentTransactionNotFound,
            ErrorConstants.ErrorCode.ExecutionCancellationError);

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Fail(expectedError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.PaymentTransactionNotFound);
        error!.Message.Should().Be("Payment transaction not found");
    }

    [Fact]
    public async Task GivenDatabaseFailure_WhenHandlerCalled_ThenReturnsTransientError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var command = CreateCommand(paymentRequestId, Stripe);

        // Domain service wraps DB errors as transient
        var wrappedError = new PaymentExecutionError(
            "Failed to get payment transaction from database: Failed to get payment transaction from DB",
            ErrorType.DependencyTransientError,
            ErrorConstants.ErrorCode.ExecutionCancellationError);

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Fail(wrappedError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        error!.Message.Should().Contain("Failed to get payment transaction from database");
    }

    [Theory, AutoData]
    public async Task GivenPaymentAlreadyCancelled_WhenHandlerCalled_ThenReturnsOk(Guid paymentRequestId,
        CancellationRequest cancellationRequest)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);

        cancellationRequest.Status = TransactionStatus.Cancelled;

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockMapper.Setup(x => x.Map<CancellationRequest>(cancellationRequest))
            .Returns(cancellationRequest);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Cancelled))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockLogger.VerifyLog(
            logger => logger.LogInformation("Payment transaction {PaymentRequestId} is already cancelled",
                paymentRequestId),
            Times.Once);

        // Validation should not be called if already cancelled
        _mockValidationService.Verify(x => x.IsPaymentTransactionCancellable(It.IsAny<CancellationRequest>()),
            Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenPaymentNotCancellable_WhenHandlerCalled_ThenReturnsValidationError(Guid paymentRequestId,
        CancellationRequest cancellationRequest)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);

        cancellationRequest.Status = TransactionStatus.Failed;

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(cancellationRequest);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Failed))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.ValidationError);
        error!.Message.Should().Contain("Payment transaction is not cancellable");
        error!.Message.Should().Contain(TransactionStatus.Failed.ToString());
    }

    [Fact]
    public async Task GivenInvalidProviderType_WhenHandlerCalled_ThenReturnsValidationError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var command = CreateCommand(paymentRequestId, "InvalidProvider");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        // Verify error is PaymentExecutionError with ValidationError type
        var error = result.Errors.FirstOrDefault() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.ValidationError);
        error!.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error!.Message.Should().Be("Invalid provider type");

        _mockLogger.VerifyLog(
            logger => logger.LogError("Invalid provider type '{ProviderType}' for PaymentRequestId {PaymentRequestId}",
                "InvalidProvider", paymentRequestId),
            Times.Once);

        // Repository should not be called if provider type is invalid
        _mockCancelDomainService.Verify(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(It.IsAny<Guid>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenValidCancellablePayment_WhenHandlerCalled_ThenReturnsOk(Guid paymentRequestId,
        CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;

        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.Submitted;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.Submitted))
            .Returns(true);

        _mockFeatureFlagClient
            .Setup(f => f.GetFeatureFlag(ExecutionConstants.FeatureFlags.EnableProviderCancellation, null))
            .Returns(new FeatureFlag<bool>
            {
                Name = "enable-payment-request-cancellation-with-provider",
                Value = true
            });

        var domainCancelRequest = new CancelPaymentRequest
        {
            TenantId = command.TenantId,
            CorrelationId = command.CorrelationId,
            PaymentRequestId = paymentRequestId,
            CancellationReason = command.CancellationReason
        };

        _mockMapper.Setup(x => x.Map<CancelPaymentRequest>(command))
            .Returns(domainCancelRequest);

        _mockCancelDomainService.Setup(x => x.HandleLambdaCancellationAsync(cancellationRequest.ProviderType, domainCancelRequest))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenProviderCancellationFeatureFlagEnabled_WhenHandlerCalled_CancelsWithProvider(
        Guid paymentRequestId, CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, "Stripe");
        cancellationRequest.ProviderType = ProviderType.Stripe;

        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.Submitted;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);
        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.Submitted))
            .Returns(true);

        _mockFeatureFlagClient
            .Setup(f => f.GetFeatureFlag(ExecutionConstants.FeatureFlags.EnableProviderCancellation, null))
            .Returns(new FeatureFlag<bool> { Name = "enable-payment-request-cancellation-with-provider", Value = true });

        var domainCancelRequest = new CancelPaymentRequest
        {
            TenantId = command.TenantId,
            CorrelationId = command.CorrelationId,
            PaymentRequestId = paymentRequestId,
            CancellationReason = command.CancellationReason
        };

        _mockMapper.Setup(x => x.Map<CancelPaymentRequest>(command))
            .Returns(domainCancelRequest);

        _mockCancelDomainService.Setup(x => x.HandleLambdaCancellationAsync(cancellationRequest.ProviderType, domainCancelRequest))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _mockGetProviderStateDomainService.Verify(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)),
            Times.Once);

        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(
                ProviderType.Stripe,
                It.Is<CancelPaymentRequest>(
                    req => req.PaymentRequestId == paymentRequestId &&
                           req.CancellationReason == command.CancellationReason)),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenProviderCancellationFeatureFlagDisabled_WhenHandlerCalled_DoesNotCancelWithProvider(
        Guid paymentRequestId, CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;

        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.Submitted;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.Submitted))
            .Returns(true);

        _mockFeatureFlagClient
            .Setup(f => f.GetFeatureFlag(ExecutionConstants.FeatureFlags.EnableProviderCancellation, null))
            .Returns(
                new FeatureFlag<bool> { Name = "enable-payment-request-cancellation-with-provider", Value = false });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockGetProviderStateDomainService.Verify(
            x => x.HandleGetProviderStateForLambdaAsync(
                It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)),
            Times.Once);
        _mockLogger.VerifyLog(
            logger =>
                logger.LogInformation("PaymentRequestId {PaymentRequestId} is submitted for cancellation with provider",
                    paymentRequestId),
            Times.Never);
        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(It.IsAny<ProviderType>(), It.IsAny<CancelPaymentRequest>()),
            Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenGetProviderStateFailsWithFailedDependency_WhenHandlerCalled_ThenReturnsFailedDependencyError(
        Guid paymentRequestId, CancellationRequest cancellationRequest)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;
        cancellationRequest.Status = TransactionStatus.Submitted;

        var failedDependencyError = new PaymentExecutionError(
            "Provider returned 403 Forbidden",
            ErrorType.FailedDependency,
            ErrorConstants.ErrorCode.ExecutionGetProviderStateError);

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        // GetProviderState fails with FailedDependency error
        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(Result.Fail(failedDependencyError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error!.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        error!.Message.Should().Be("Provider returned 403 Forbidden");

        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(It.IsAny<ProviderType>(), It.IsAny<CancelPaymentRequest>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenGetProviderStateFailsWithDependencyTransientError_WhenHandlerCalled_ThenReturnsFail(
        Guid paymentRequestId, CancellationRequest cancellationRequest)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;
        cancellationRequest.Status = TransactionStatus.Submitted;

        var expectedError = new PaymentExecutionError(
            "Service unavailable",
            ErrorType.DependencyTransientError,
            ErrorConstants.ErrorCode.ExecutionGetProviderStateError);

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(Result.Fail(expectedError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Service unavailable");

        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(It.IsAny<ProviderType>(), It.IsAny<CancelPaymentRequest>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenPaymentInRequiresActionState_WhenHandlerCalled_ThenReturnsValidationError(
        Guid paymentRequestId, CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;
        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.RequiresAction;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.RequiresAction))
            .Returns(false);

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.ValidationError);
        error!.Message.Should().Contain("Payment is not auto-cancellable");
        error!.Message.Should().Contain(PaymentProviderStatus.RequiresAction.ToString());

        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(It.IsAny<ProviderType>(), It.IsAny<CancelPaymentRequest>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenPaymentInTerminalState_WhenHandlerCalled_ThenReturnsValidationError(
        Guid paymentRequestId, CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;
        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.Terminal;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.Terminal))
            .Returns(false);

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.ValidationError);
        error!.Message.Should().Contain("Payment is not auto-cancellable");
        error!.Message.Should().Contain(PaymentProviderStatus.Terminal.ToString());

        _mockCancelDomainService.Verify(
            x => x.HandleLambdaCancellationAsync(It.IsAny<ProviderType>(), It.IsAny<CancelPaymentRequest>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenProviderCancellationFails_WhenHandlerCalled_ThenReturnsFail(
        Guid paymentRequestId, CancellationRequest cancellationRequest, ProviderState providerState)
    {
        // Arrange
        var command = CreateCommand(paymentRequestId, Stripe);
        cancellationRequest.ProviderType = ProviderType.Stripe;
        cancellationRequest.Status = TransactionStatus.Submitted;
        providerState.PaymentProviderStatus = PaymentProviderStatus.Submitted;

        _mockMapper.Setup(x => x.Map<GetProviderStateRequest>(command))
            .Returns(new GetProviderStateRequest
            {
                ProviderType = command.ProviderType,
                PaymentRequestId = command.PaymentRequestId,
                CorrelationId = command.CorrelationId,
                TenantId = command.TenantId
            });

        _mockCancelDomainService.Setup(x => x.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId))
            .ReturnsAsync(Result.Ok(cancellationRequest));

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancelled(TransactionStatus.Submitted))
            .Returns(false);

        _mockValidationService.Setup(x => x.IsPaymentTransactionCancellable(cancellationRequest))
            .Returns(true);

        _mockValidationService.Setup(x => x.IsPaymentAutoCancellable(PaymentProviderStatus.Submitted))
            .Returns(true);

        _mockGetProviderStateDomainService.Setup(x => x.HandleGetProviderStateForLambdaAsync(
            It.Is<GetProviderStateRequest>(r => r.ProviderType == Stripe && r.PaymentRequestId == paymentRequestId)))
            .ReturnsAsync(providerState);

        _mockFeatureFlagClient
            .Setup(f => f.GetFeatureFlag(ExecutionConstants.FeatureFlags.EnableProviderCancellation, null))
            .Returns(new FeatureFlag<bool> { Name = "enable-payment-request-cancellation-with-provider", Value = true });

        var domainCancelRequest = new CancelPaymentRequest
        {
            TenantId = command.TenantId,
            CorrelationId = command.CorrelationId,
            PaymentRequestId = paymentRequestId,
            CancellationReason = command.CancellationReason
        };

        _mockMapper.Setup(x => x.Map<CancelPaymentRequest>(command))
            .Returns(domainCancelRequest);

        _mockCancelDomainService
            .Setup(x => x.HandleLambdaCancellationAsync(ProviderType.Stripe, It.IsAny<CancelPaymentRequest>()))
            .ReturnsAsync(Result.Fail("Provider cancellation failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Provider cancellation failed");
    }

    private static ProcessCancelMessageCommand CreateCommand(Guid paymentRequestId, string providerType)
    {
        return new ProcessCancelMessageCommand
        {
            TenantId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            PaymentRequestId = paymentRequestId,
            ProviderType = providerType,
            CancellationReason = "Test cancellation"
        };
    }
}
