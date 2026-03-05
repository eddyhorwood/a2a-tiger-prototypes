using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Specifications.CancellationSpecification;
using TransactionStatus = PaymentExecution.Domain.Models.TransactionStatus;

namespace PaymentExecution.Domain.UnitTests.Specifications.CancellationSpecification;

public class EligibleStatusSpecificationTests
{
    private readonly EligibleStatusSpecification _sut;
    public EligibleStatusSpecificationTests()
    {
        var mockLogger = new Mock<ILogger<EligibleStatusSpecification>>();
        _sut = new EligibleStatusSpecification(mockLogger.Object);
    }

    [Fact]
    public void GivenSubmittedStatus_WhenSpecificationIsCancellableInvoked_ThenReturnsTrue()
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = TransactionStatus.Submitted,
            ProviderType = ProviderType.Stripe
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(TransactionStatus.Failed)]
    [InlineData(TransactionStatus.Succeeded)]
    public void GivenStatusNotEligibleForCancellation_WhenSpecificationIsCancellableInvoked_ThenReturnsFalse(TransactionStatus status)
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = status,
            ProviderType = ProviderType.Stripe
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeFalse();
    }
}
