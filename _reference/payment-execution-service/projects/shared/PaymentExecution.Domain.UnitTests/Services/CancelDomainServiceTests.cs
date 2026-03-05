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
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;

namespace PaymentExecution.Domain.UnitTests.Services;

public class CancelDomainServiceTests
{
    private readonly CancelDomainService _sut;
    private readonly Mock<IPaymentTransactionRepository> _mockRepository = new();
    private readonly Mock<IProviderIntegrationDomainServiceFactory> _mockFactory = new();
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<ILogger<CancelDomainService>> _mockLogger = new();
    private readonly Mock<IProviderIntegrationDomainService> _mockProviderIntegrationService = new();

    public CancelDomainServiceTests()
    {
        _sut = new CancelDomainService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockFactory.Object,
            _mockLogger.Object);
    }

    #region HandleGetPaymentTransactionRecordAsync

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsFailed_WhenHandleGetPaymentTransactionRecordAsyncInvoked_ThenReturnsResultFailed(Guid paymentRequestId)
    {
        //Arrange
        var expectedErrorMessage = "Error retrieving record";
        var mockedResult = Result.Fail(expectedErrorMessage);
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Fail(expectedErrorMessage));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsync(paymentRequestId);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedResult);
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetPaymentTransactionRecordAsyncInvoked_ThenReturnsResultFailed(Guid paymentRequestId)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsync(paymentRequestId);

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetPaymentTransactionRecordAsyncInvoked_ThenReturnsCorrectPaymentExecutionError(Guid paymentRequestId)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsync(paymentRequestId);

        //Assert
        result.Errors.First().Should().BeOfType<PaymentExecutionError>();
        var errorType = ((PaymentExecutionError)result.Errors.First()).GetErrorType();
        errorType.Should().Be(ErrorType.PaymentTransactionNotFound);
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetPaymentTransactionRecordAsyncInvoked_ThenLogsInformation(Guid paymentRequestId)
    {
        //Arrange
        var expectedLogMessage = $"No payment transaction found for payment request id {paymentRequestId}";
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        await _sut.HandleGetPaymentTransactionRecordAsync(paymentRequestId);

        //Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Information, expectedLogMessage, 1);
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsPaymentTransactionDto_WhenHandleGetPaymentTransactionRecordAsyncInvoked_ThenReturnsResultWithExpectedPayload(
        PaymentTransactionDto mockedDto, CancellationRequest mockedCancellationRequest, Guid paymentRequestId)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok((PaymentTransactionDto?)mockedDto));
        _mockMapper.Setup(m => m.Map<CancellationRequest>(mockedDto))
            .Returns(mockedCancellationRequest);

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsync(paymentRequestId);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockedCancellationRequest);
    }

    #endregion

    #region HandleGetPaymentTransactionRecordAsyncForLambda

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsPlainStringError_WhenHandleGetPaymentTransactionRecordAsyncForLambdaInvoked_ThenReturnsWrappedDependencyTransientError(Guid paymentRequestId)
    {
        //Arrange
        var plainStringError = "Database connection failed";
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Fail(plainStringError));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Contain("Failed to get payment transaction from database");
        error.Message.Should().Contain(plainStringError);
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsPaymentExecutionError_WhenHandleGetPaymentTransactionRecordAsyncForLambdaInvoked_ThenReturnsOriginalError(Guid paymentRequestId)
    {
        //Arrange
        var originalError = new PaymentExecutionError(
            "Original error message",
            ErrorType.FailedDependency,
            ErrorConstants.ErrorCode.ExecutionCancellationError);
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Fail(originalError));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Original error message");
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetPaymentTransactionRecordAsyncForLambdaInvoked_ThenReturnsPaymentTransactionNotFoundError(Guid paymentRequestId)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.PaymentTransactionNotFound);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Payment transaction not found");
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsPaymentTransactionDto_WhenHandleGetPaymentTransactionRecordAsyncForLambdaInvoked_ThenReturnsResultWithExpectedPayload(
        PaymentTransactionDto mockedDto, CancellationRequest mockedCancellationRequest, Guid paymentRequestId)
    {
        //Arrange
        _mockRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok((PaymentTransactionDto?)mockedDto));
        _mockMapper.Setup(m => m.Map<CancellationRequest>(mockedDto))
            .Returns(mockedCancellationRequest);

        //Act
        var result = await _sut.HandleGetPaymentTransactionRecordAsyncForLambda(paymentRequestId);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockedCancellationRequest);
    }

    #endregion

    #region HandleSyncCancellationAsync
    [Theory, AutoData]
    public async Task GivenFactoryGetProviderIntegrationDomainServiceFails_WhenHandleSyncCancellationAsync_ThenPropagatesResultFail(
        CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedFactoryResult = Result.Fail("whoops something happened");
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(cancellationRequest.ProviderType.ToString()))
            .Returns(mockedFactoryResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedFactoryResult);
    }

    [Theory, AutoData]
    public async Task GivenProviderDomainServiceReturnsGenericResultFail_WhenHandleSyncCancellationAsync_ThenPropagatesResultFail(
        CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedFailedProviderCancelResult = Result.Fail("whoops something happened");
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedFailedProviderCancelResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().Be(mockedFailedProviderCancelResult);
    }

    [Theory, AutoData]
    public async Task GivenProviderServiceReturnsResultFailWithoutStatusCode_WhenHandleSyncCancellationAsync_ThenReturnsGenericResultFail(
        CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedErrorWithoutStatusCode = new PaymentExecutionError("only a message!!");
        var mockedResult = Result.Fail(mockedErrorWithoutStatusCode);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var expectedResult = Result.Fail("No status code in error from provider client.");
        result.Should().BeEquivalentTo(expectedResult);
    }

    /// <summary>
    /// Bad request without a provider error code suggests some issue with our code: therefore 500 is appropriate
    /// </summary>
    [Theory, AutoData]
    public async Task GivenProviderServiceReturnsBadRequestWithNoProviderErrorCode_WhenHandleSyncCancellationAsync_ThenReturnsGenericResultFail(
        CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedBadRequestError = new PaymentExecutionError(
            "You have some issue with Authorization/config!",
            providerErrorCode: null,
            HttpStatusCode.BadRequest);
        var mockedResult = Result.Fail(mockedBadRequestError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var expectedResult = Result.Fail("Authorization error when integrating with provider specific service.");
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.Unauthorized)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    public async Task GivenProviderServiceReturnsResultFailWithAuthIssueStatusCode_WhenHandleSyncCancellationAsync_ThenReturnsGenericResultFail(
        HttpStatusCode mockedStatusCode, CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedBadRequestError = new PaymentExecutionError(
            "You have some misconfiguration!",
            mockedStatusCode);
        var mockedResult = Result.Fail(mockedBadRequestError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var expectedResult = Result.Fail("Authorization error when integrating with provider specific service.");
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.PaymentRequired)]
    [InlineAutoData(HttpStatusCode.UnprocessableContent)]
    [InlineAutoData(HttpStatusCode.FailedDependency)]
    public async Task GivenProviderServiceReturnsResultFailWithNonTransientStatusCodeAndProviderErrorCode_WhenHandleSyncCancellationAsync_ThenReturnsExpectedNotCancelableError(
        HttpStatusCode mockedStatusCode, CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedProviderErrorCode = "provider-error-code";
        var mockedErrorMessage = "Non transient error occurred!";
        var mockedBadRequestError = new PaymentExecutionError(
            mockedErrorMessage,
            mockedProviderErrorCode,
            mockedStatusCode);
        var mockedResult = Result.Fail(mockedBadRequestError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<PaymentExecutionError>();
        var returnedError = (PaymentExecutionError)result.Errors[0];

        returnedError.Message.Should().Be(mockedErrorMessage);
        returnedError.GetErrorType().Should().Be(ErrorType.PaymentTransactionNotCancellable);
        returnedError.GetProviderErrorCode().Should().Be(mockedProviderErrorCode);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.TooManyRequests)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.ServiceUnavailable)]
    public async Task GivenProviderServiceReturnsResultFailWithTransientErrorStatusCode_WhenHandleSyncCancellationAsync_ThenReturnsExpectedErrorOfTypeDependencyTransientError(
        HttpStatusCode mockedStatusCode, CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedProviderErrorCode = "provider-error-code";
        var mockedErrorMessage = "Transient error occurred!";
        var mockedBadRequestError = new PaymentExecutionError(
            mockedErrorMessage,
            mockedProviderErrorCode,
            mockedStatusCode);
        var mockedResult = Result.Fail(mockedBadRequestError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<PaymentExecutionError>();
        var returnedError = (PaymentExecutionError)result.Errors[0];

        returnedError.Message.Should().Be(mockedErrorMessage);
        returnedError.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        returnedError.GetProviderErrorCode().Should().Be(mockedProviderErrorCode);
    }

    [Theory, AutoData]
    public async Task GivenProviderDomainServiceCancelSuccess_WhenHandleSyncCancellationAsync_ThenReturnsResultOk(
        CancellationRequest cancellationRequest, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedProviderCancelResult = Result.Ok();
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedProviderCancelResult);

        //Act
        var result = await _sut.HandleSyncCancellationAsync(cancellationRequest.ProviderType, mockCancelPaymentRequest);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedProviderCancelResult);
    }

    #endregion

    #region HandleLambdaCancellationAsync

    [Theory, AutoData]
    public async Task GivenFactoryGetProviderIntegrationDomainServiceFails_WhenHandleLambdaCancellationAsync_ThenReturnsFailedDependency(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedFactoryResult = Result.Fail("Factory error");
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(mockedFactoryResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Contain("Failed to cancel payment with provider");
    }

    [Theory, AutoData]
    public async Task GivenProviderReturnsGenericError_WhenHandleLambdaCancellationAsync_ThenReturnsFailedDependencyError(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedFailedProviderCancelResult = Result.Fail("Generic string error");
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedFailedProviderCancelResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Failed to cancel payment with provider: Generic string error");
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.Unauthorized)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.NotFound)]
    [InlineAutoData(HttpStatusCode.PaymentRequired)]
    [InlineAutoData(HttpStatusCode.UnprocessableContent)]
    public async Task GivenProviderReturnsNonTransientStatusCode_WhenHandleLambdaCancellationAsync_ThenReturnsFailedDependencyError(
        HttpStatusCode statusCode, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedError = new PaymentExecutionError("Provider error", httpStatusCode: statusCode);
        var mockedResult = Result.Fail(mockedError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Provider error");
        error.GetHttpStatusCode().Should().Be(statusCode);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.TooManyRequests)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.BadGateway)]
    [InlineAutoData(HttpStatusCode.ServiceUnavailable)]
    [InlineAutoData(HttpStatusCode.GatewayTimeout)]
    public async Task GivenProviderReturnsTransientStatusCode_WhenHandleLambdaCancellationAsync_ThenReturnsDependencyTransientError(
        HttpStatusCode statusCode, CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedError = new PaymentExecutionError("Transient error", httpStatusCode: statusCode);
        var mockedResult = Result.Fail(mockedError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Transient error");
        error.GetHttpStatusCode().Should().Be(statusCode);
    }

    [Theory, AutoData]
    public async Task GivenProviderReturnsPaymentExecutionErrorWithNullStatusCode_WhenHandleLambdaCancellationAsync_ThenReturnsDependencyTransientError(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedError = new PaymentExecutionError("Error without status code", httpStatusCode: null);
        var mockedResult = Result.Fail(mockedError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.Message.Should().Be("Error without status code");
        error.GetHttpStatusCode().Should().BeNull();
    }

    [Theory, AutoData]
    public async Task GivenProviderReturnsErrorWithProviderErrorCode_WhenHandleLambdaCancellationAsync_ThenPreservesProviderErrorCode(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedError = new PaymentExecutionError(
            "Provider specific error",
            providerErrorCode: "stripe_invalid_card",
            httpStatusCode: HttpStatusCode.BadRequest);
        var mockedResult = Result.Fail(mockedError);
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionCancellationError);
        error.GetProviderErrorCode().Should().Be("stripe_invalid_card");
        error.Message.Should().Be("Provider specific error");
    }

    [Theory, AutoData]
    public async Task GivenProviderCancellationSucceeds_WhenHandleLambdaCancellationAsync_ThenReturnsResultOk(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        //Arrange
        var mockedProviderCancelResult = Result.Ok();
        _mockFactory.Setup(m => m.GetProviderIntegrationDomainService(nameof(ProviderType.Stripe)))
            .Returns(Result.Ok(_mockProviderIntegrationService.Object));
        _mockProviderIntegrationService.Setup(m => m.CancelPaymentAsync(mockCancelPaymentRequest))
            .ReturnsAsync(mockedProviderCancelResult);

        //Act
        var result = await _sut.HandleLambdaCancellationAsync(ProviderType.Stripe, mockCancelPaymentRequest);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedProviderCancelResult);
    }

    #endregion
}
