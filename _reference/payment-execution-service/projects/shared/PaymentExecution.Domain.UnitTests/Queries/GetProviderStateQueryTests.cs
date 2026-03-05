using AutoFixture.Xunit2;
using FluentAssertions;
using FluentResults;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Queries;
using PaymentExecution.Domain.Service;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.UnitTests.Queries;

public class GetProviderStateQueryTests
{
    private readonly GetProviderStateQueryHandler _sut;
    private readonly Mock<IGetProviderStateDomainService> _getProviderStateDomainService = new();

    public GetProviderStateQueryTests()
    {
        _sut = new GetProviderStateQueryHandler(
                _getProviderStateDomainService.Object);
    }

    [Theory, AutoData]
    public async Task GivenValidPaymentRequestIdAndNoErrors_WhenHandleIsCalled_ThenReturnsExpectedProviderState(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var query = new GetProviderStateQuery
        {
            PaymentRequestId = paymentRequestId
        };
        var expectedProviderState = new ProviderState()
        {
            PaymentProviderStatus = PaymentProviderStatus.RequiresAction,
            PaymentProviderPaymentTransactionId = "pi_12345hteas",
            ProviderType = ProviderType.Stripe,
            LastPaymentErrorCode = null,
            PendingStatusDetails = new PendingStatusDetails
            {
                RequiresActionType = RequiresActionType.RedirectToUrl,
                RedirectToUrl = new RedirectToUrl() { RedirectUrl = "https://example.com/redirect", }
            }
        };
        _getProviderStateDomainService.Setup(m => m.HandleGetPaymentTransactionById(paymentRequestId))
            .ReturnsAsync(Result.Ok(mockedPaymentTransactionDto));
        _getProviderStateDomainService.Setup(m => m.HandleGetProviderStateAsync(mockedPaymentTransactionDto.ProviderType, paymentRequestId))
            .ReturnsAsync(Result.Ok(expectedProviderState));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.ProviderState.Should().BeSameAs(expectedProviderState);
    }

    [Fact]
    public async Task GivenHandleGetPaymentTransactionIdReturnsResultFail_WhenHandleIsCalled_ThenPropagatesReturnedError()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedFailedResult = Result.Fail<PaymentTransactionDto>("Some error occurred");
        var query = new GetProviderStateQuery
        {
            PaymentRequestId = paymentRequestId
        };
        _getProviderStateDomainService.Setup(m => m.HandleGetPaymentTransactionById(paymentRequestId))
            .ReturnsAsync(mockedFailedResult)
            .Verifiable();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().BeEquivalentTo(mockedFailedResult.Errors);
        _getProviderStateDomainService.Verify();
    }

    [Theory, AutoData]
    public async Task GivenHandleGetProviderStateReturnsResultFail_WhenHandleIsCalled_ThenPropagatesReturnedError(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedFailedResult = Result.Fail("Some error occurred");
        var query = new GetProviderStateQuery
        {
            PaymentRequestId = paymentRequestId
        };
        _getProviderStateDomainService.Setup(m => m.HandleGetPaymentTransactionById(paymentRequestId))
            .ReturnsAsync(mockedPaymentTransactionDto)
            .Verifiable();
        _getProviderStateDomainService.Setup(m =>
                m.HandleGetProviderStateAsync(mockedPaymentTransactionDto.ProviderType, paymentRequestId))
            .ReturnsAsync(mockedFailedResult)
            .Verifiable();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedFailedResult);
        _getProviderStateDomainService.Verify();
    }
}
