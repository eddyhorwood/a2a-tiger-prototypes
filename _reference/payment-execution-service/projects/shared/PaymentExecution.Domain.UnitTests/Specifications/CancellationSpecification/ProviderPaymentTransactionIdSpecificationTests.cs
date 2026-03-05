using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Specifications.CancellationSpecification;

namespace PaymentExecution.Domain.UnitTests.Specifications.CancellationSpecification;

public class ProviderPaymentTransactionIdSpecificationTests
{
    private readonly ProviderPaymentTransactionIdSpecification _sut;
    public ProviderPaymentTransactionIdSpecificationTests()
    {
        var mockLogger = new Mock<ILogger<ProviderPaymentTransactionIdSpecification>>();
        _sut = new ProviderPaymentTransactionIdSpecification(mockLogger.Object);
    }

    [Fact]
    public void GivenProviderPaymentTransactionIdIsValidString_WhenSpecificationIsCancellableInvoked_ThenReturnsTrue()
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = TransactionStatus.Submitted,
            ProviderType = ProviderType.Stripe,
            PaymentProviderPaymentTransactionId = "pi_12345"
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void GivenNullOrWhitespaceProviderPaymentTransactionId_WhenSpecificationIsCancellableInvoked_ThenReturnsFalse(string? invalidPaymentIntentid)
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = TransactionStatus.Submitted,
            ProviderType = ProviderType.Stripe,
            PaymentProviderPaymentTransactionId = invalidPaymentIntentid
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeFalse();
    }
}
