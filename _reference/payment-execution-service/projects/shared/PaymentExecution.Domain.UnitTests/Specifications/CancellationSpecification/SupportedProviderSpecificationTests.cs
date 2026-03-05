using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Specifications.CancellationSpecification;

namespace PaymentExecution.Domain.UnitTests.Specifications.CancellationSpecification;

public class SupportedProviderSpecificationTests
{
    private readonly SupportedProviderSpecification _sut;
    public SupportedProviderSpecificationTests()
    {
        var mockLogger = new Mock<ILogger<SupportedProviderSpecification>>();
        _sut = new SupportedProviderSpecification(mockLogger.Object);
    }

    [Fact]
    public void GivenStripeProviderType_WhenSpecificationIsCancellableInvoked_ThenReturnsTrue()
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = TransactionStatus.Submitted,
            ProviderType = ProviderType.Stripe,
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProviderType.Paypal)]
    [InlineData(ProviderType.GoCardless)]
    public void GivenNotSupportedProviderType_WhenSpecificationIsCancellableInvoked_ThenReturnsFalse(ProviderType providerType)
    {
        //Arrange
        var cancellationRequest = new CancellationRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = TransactionStatus.Submitted,
            ProviderType = providerType,
        };

        //Act
        var result = _sut.IsCancellable(cancellationRequest);

        //Assert
        result.Should().BeFalse();
    }
}
