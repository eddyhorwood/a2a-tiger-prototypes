using AutoFixture.Xunit2;
using FluentAssertions;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.Domain.Specifications.CancellationSpecification;

namespace PaymentExecution.Domain.UnitTests.Services;

public class CancellationValidationServiceTests
{

    [Theory]
    [InlineData(TransactionStatus.Cancelled)]
    public void GivenStatusIsCancelled_WhenIsPaymentTransactionCancelledInvoked_ThenReturnsTrue(TransactionStatus status)
    {
        // Arrange
        var sut = new CancellationValidationService(new List<ICancellationSpecification>());

        // Act
        var result = sut.IsPaymentTransactionCancelled(status);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(TransactionStatus.Succeeded)]
    [InlineData(TransactionStatus.Failed)]
    [InlineData(TransactionStatus.Submitted)]
    public void GivenStatusIsNotCancelled_WhenIsPaymentTransactionCancelledInvoked_ThenReturnsFalse(TransactionStatus mockedStatus)
    {
        // Arrange
        var sut = new CancellationValidationService(new List<ICancellationSpecification>());

        // Act
        var result = sut.IsPaymentTransactionCancelled(mockedStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public void GivenAllSpecificationsReturnTrue_WhenIsPaymentTransactionCancellableInvoked_ThenReturnsTrue(CancellationRequest cancellationRequest)
    {
        // Arrange
        var mockSpecifications = new List<ICancellationSpecification>
        {
            new MockCancellationSpecification(true),
            new MockCancellationSpecification(true)
        };
        var sut = new CancellationValidationService(mockSpecifications);

        // Act
        var result = sut.IsPaymentTransactionCancellable(cancellationRequest);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void GivenAllSpecificationsReturnFalse_WhenIsPaymentTransactionCancellableInvoked_ThenReturnsFalse(CancellationRequest cancellationRequest)
    {
        // Arrange
        var mockSpecifications = new List<ICancellationSpecification>
        {
            new MockCancellationSpecification(false),
            new MockCancellationSpecification(false)
        };
        var sut = new CancellationValidationService(mockSpecifications);

        // Act
        var result = sut.IsPaymentTransactionCancellable(cancellationRequest);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public void GivenOneSpecificationReturnsFalse_WhenIsPaymentTransactionCancellableInvoked_ThenReturnsFalse(CancellationRequest cancellationRequest)
    {
        // Arrange
        var mockSpecifications = new List<ICancellationSpecification>
        {
            new MockCancellationSpecification(false),
            new MockCancellationSpecification(true)
        };
        var sut = new CancellationValidationService(mockSpecifications);

        // Act
        var result = sut.IsPaymentTransactionCancellable(cancellationRequest);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(PaymentProviderStatus.Submitted)]
    public void GivenAutoCancellableProviderStatus_WhenIsPaymentAutoCancellableInvoked_ThenReturnsTrue(PaymentProviderStatus status)
    {
        // Arrange
        var sut = new CancellationValidationService(new List<ICancellationSpecification>());

        // Act
        var result = sut.IsPaymentAutoCancellable(status);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(PaymentProviderStatus.RequiresAction)]
    [InlineData(PaymentProviderStatus.Terminal)]
    [InlineData(PaymentProviderStatus.Processing)]
    public void GivenNonAutoCancellableProviderStatus_WhenIsPaymentAutoCancellableInvoked_ThenReturnsFalse(PaymentProviderStatus status)
    {
        // Arrange
        var sut = new CancellationValidationService(new List<ICancellationSpecification>());

        // Act
        var result = sut.IsPaymentAutoCancellable(status);

        // Assert
        result.Should().BeFalse();
    }

    private class MockCancellationSpecification(bool isCancellable) : ICancellationSpecification
    {
        public bool IsCancellable(CancellationRequest cancellationRequest)
        {
            return isCancellable;
        }
    }
}
