using System.Net;
using AutoFixture.Xunit2;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.UnitTests.Services;

public class GetProviderStateDomainServiceTests
{
    private readonly GetProviderStateDomainService _sut;
    private readonly Mock<IPaymentTransactionRepository> _repository = new();
    private readonly Mock<IProviderIntegrationDomainServiceFactory> _providerIntegrationDomainServiceFactory = new();
    private readonly Mock<ILogger<GetProviderStateDomainService>> _logger = new();
    private readonly Mock<IProviderIntegrationDomainService> _providerIntegrationService = new();

    public GetProviderStateDomainServiceTests()
    {
        _sut = new GetProviderStateDomainService(
            _repository.Object,
            _providerIntegrationDomainServiceFactory.Object,
            _logger.Object);
    }

    [Theory, AutoData]
    public async Task GivenRepositoryReturnsTransaction_WhenHandleGetPaymentTransactionByIdIsCalled_ThenReturnsExpectedTransaction(
        PaymentTransactionDto transactionDto)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        _repository.Setup(x => x.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(transactionDto));

        // Act
        var result = await _sut.HandleGetPaymentTransactionById(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(transactionDto);
    }

    [Fact]
    public async Task GivenRepositoryReturnsResultFail_WhenHandleGetPaymentTransactionByIdIsCalled_ThenReturnsFail()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedFailResult = Result.Fail("Some database error");
        _repository.Setup(x => x.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(mockedFailResult);

        // Act
        var result = await _sut.HandleGetPaymentTransactionById(paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(mockedFailResult.Errors);
    }

    [Fact]
    public async Task GivenRepositoryReturnsNull_WhenHandleGetPaymentTransactionByIdIsCalled_ThenReturnsPaymentTransactionNotFoundError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var expectedError = new PaymentExecutionError(
            ErrorConstants.ErrorMessage.PaymentTransactionNotFoundError,
            ErrorType.PaymentTransactionNotFound,
            ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        _repository.Setup(x => x.GetPaymentTransactionsByPaymentRequestId(paymentRequestId))
            .ReturnsAsync(Result.Ok<PaymentTransactionDto?>(null));

        // Act
        var result = await _sut.HandleGetPaymentTransactionById(paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PaymentExecutionError;
        error.Should().NotBeNull();
        error.Should().BeEquivalentTo(expectedError);
    }

    [Fact]
    public async Task GivenProviderFactoryReturnsResultFail_HandleGetProviderStateAsync_ThenPropagatesReturnedError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedProviderType = "Stripe";
        var mockedFailedResult = Result.Fail("oh dear!");
        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(mockedProviderType))
            .Returns(mockedFailedResult);

        // Act
        var result = await _sut.HandleGetProviderStateAsync(mockedProviderType, paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedFailedResult);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.FailedDependency)]
    [InlineData(HttpStatusCode.PaymentRequired, ErrorType.FailedDependency)]
    [InlineData(HttpStatusCode.Conflict, ErrorType.FailedDependency)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorType.DependencyTransientError)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.DependencyTransientError)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorType.DependencyTransientError)]
    public async Task
        GivenProviderIntegrationServiceReturnsExecutionErrorWithStatusCode_WhenHandleGetProviderStateAsync_ThenReturnsExpectedMappedError(
            HttpStatusCode mockStatusCode,
            ErrorType expectedErrorType)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedProviderType = "Stripe";
        var mockedProviderErrorCode = "fake-provider-error-code";
        var mockedErrorMessage = "something gone bad :c";
        var mockedExecutionError =
            new PaymentExecutionError(mockedErrorMessage, mockedProviderErrorCode, mockStatusCode);
        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(mockedProviderType))
            .Returns(Result.Ok(_providerIntegrationService.Object));
        _providerIntegrationService.Setup(m => m.GetProviderStateAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Fail(mockedExecutionError));

        // Act
        var result = await _sut.HandleGetProviderStateAsync(mockedProviderType, paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        var expectedError = new PaymentExecutionError(
            mockedErrorMessage,
            mockedProviderErrorCode,
            mockStatusCode);
        expectedError.SetErrorCode(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        expectedError.SetErrorType(expectedErrorType);

        result.Errors[0].Should().BeEquivalentTo(expectedError);
    }

    [Theory, AutoData]
    public async Task GivenSuccessfulGetFromProviderIntegrationService_WhenHandleGetProviderStateAsync_ThenReturnsExpectedProviderState(
        ProviderState mockedProviderState)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedProviderType = "Stripe";
        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(mockedProviderType))
            .Returns(Result.Ok(_providerIntegrationService.Object));
        _providerIntegrationService.Setup(m => m.GetProviderStateAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(mockedProviderState);

        // Act
        var result = await _sut.HandleGetProviderStateAsync(mockedProviderType, paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockedProviderState);
    }

    [Theory, AutoData]
    public async Task GivenHeadersProvided_WhenHandleGetProviderStateForLambdaAsync_ThenPassesHeadersToProviderService(
        GetProviderStateRequest getProviderStateRequest,
        ProviderState mockedProviderState)
    {
        // Arrange
        getProviderStateRequest.ProviderType = "Stripe";

        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(getProviderStateRequest.ProviderType))
            .Returns(Result.Ok(_providerIntegrationService.Object));
        _providerIntegrationService.Setup(m => m.GetProviderStateAsync(getProviderStateRequest.PaymentRequestId, getProviderStateRequest.CorrelationId, getProviderStateRequest.TenantId))
            .ReturnsAsync(mockedProviderState);

        // Act
        var result = await _sut.HandleGetProviderStateForLambdaAsync(getProviderStateRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockedProviderState);
        _providerIntegrationService.Verify(
            m => m.GetProviderStateAsync(getProviderStateRequest.PaymentRequestId, getProviderStateRequest.CorrelationId, getProviderStateRequest.TenantId),
            Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenProviderFactoryFails_WhenHandleGetProviderStateForLambdaAsync_ThenReturnsFailedDependencyError(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        // Arrange
        var mockedProviderType = "Stripe";
        var request = new GetProviderStateRequest
        {
            ProviderType = mockedProviderType,
            PaymentRequestId = paymentRequestId,
            CorrelationId = correlationId,
            TenantId = tenantId
        };

        var mockedFailedResult = Result.Fail("Provider factory error");
        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(mockedProviderType))
            .Returns(mockedFailedResult);

        // Act
        var result = await _sut.HandleGetProviderStateForLambdaAsync(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.First() as PaymentExecutionError;
        error.Should().NotBeNull();
        error!.GetErrorType().Should().Be(ErrorType.FailedDependency);
        error.Message.Should().Be("Failed to get provider integration service");
    }

    [Theory, AutoData]
    public async Task GivenProviderIntegrationServiceFails_WhenHandleGetProviderStateForLambdaAsync_ThenReturnsErrorMapped(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        // Arrange
        var mockedProviderType = "Stripe";
        var request = new GetProviderStateRequest
        {
            ProviderType = mockedProviderType,
            PaymentRequestId = paymentRequestId,
            CorrelationId = correlationId,
            TenantId = tenantId
        };

        var mockedProviderErrorCode = "provider-error";
        var mockedErrorMessage = "Provider service failed";
        var mockedExecutionError = new PaymentExecutionError(
            mockedErrorMessage,
            mockedProviderErrorCode,
            HttpStatusCode.InternalServerError);

        _providerIntegrationDomainServiceFactory.Setup(m => m.GetProviderIntegrationDomainService(mockedProviderType))
            .Returns(Result.Ok(_providerIntegrationService.Object));
        _providerIntegrationService.Setup(m => m.GetProviderStateAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(Result.Fail(mockedExecutionError));

        // Act
        var result = await _sut.HandleGetProviderStateForLambdaAsync(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PaymentExecutionError;
        error.Should().NotBeNull();
        error.GetErrorCode().Should().Be(ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
        error.GetErrorType().Should().Be(ErrorType.DependencyTransientError);
    }
}
