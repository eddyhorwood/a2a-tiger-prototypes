using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.TestUtilities;

namespace PaymentExecution.Domain.UnitTests.Services;

public class ProviderIntegrationDomainServiceFactoryTests
{
    private readonly IProviderIntegrationDomainServiceFactory _sut;
    private readonly Mock<ILogger<ProviderIntegrationDomainServiceFactory>> _loggerMock = new();
    private readonly Mock<IProviderIntegrationDomainService> _stripeProviderHandlerMock = new();

    public ProviderIntegrationDomainServiceFactoryTests()
    {
        _stripeProviderHandlerMock.Setup(x => x.ProviderType).Returns(ProviderType.Stripe);

        _sut = new ProviderIntegrationDomainServiceFactory([_stripeProviderHandlerMock.Object], _loggerMock.Object);
    }

    [Theory]
    [InlineData(nameof(ProviderType.Stripe))]
    [InlineData("stripe")]
    public void GivenStripeProviderType_WhenGetProviderHandler_ReturnsExpectedStripeProviderHandler(string mockedProviderType)
    {
        // Arrange
        var expectedStripeProvider = ProviderType.Stripe;

        // Act
        var result = _sut.GetProviderIntegrationDomainService(mockedProviderType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var providerIntegrationDomainService = result.Value;
        providerIntegrationDomainService.ProviderType.Should().Be(expectedStripeProvider);
    }

    [Fact]
    public void GivenInvalidProviderType_WhenGetProviderHandler_ReturnsResultFailAndLogsError()
    {
        // Arrange
        var mockedProviderType = "InvalidAndFakeProvider";
        var expectedErrorLog = $"Provider type {mockedProviderType} not supported";

        // Act
        var result = _sut.GetProviderIntegrationDomainService(mockedProviderType);

        // Assert
        result.IsFailed.Should().BeTrue();
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_loggerMock, LogLevel.Error, expectedErrorLog, 1);
    }
}
