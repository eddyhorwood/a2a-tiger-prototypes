using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.UnitTests.Mapping;

public class RepositoryToDomainMappingTests
{
    private readonly IMapper _sut;

    public RepositoryToDomainMappingTests()
    {
        var configuration = new MapperConfiguration(cfg => { cfg.AddProfile(new RepositoryToDomainMappingProfile()); });
        configuration.AssertConfigurationIsValid();

        _sut = configuration.CreateMapper();
    }

    [Theory]
    [InlineAutoData("submitted", TransactionStatus.Submitted)]
    [InlineAutoData("Submitted", TransactionStatus.Submitted)]
    [InlineAutoData("Failed", TransactionStatus.Failed)]
    [InlineAutoData("failed", TransactionStatus.Failed)]
    [InlineAutoData("Succeeded", TransactionStatus.Succeeded)]
    [InlineAutoData("succeeded", TransactionStatus.Succeeded)]
    [InlineAutoData("Cancelled", TransactionStatus.Cancelled)]
    [InlineAutoData("cancelled", TransactionStatus.Cancelled)]
    public void GivenDtoStatusIsValidTransactionStatusString_WhenAutoMappedToCancellationRequest_ThenStatusMappedAsExpected(
        string status, TransactionStatus expectedDomainStatus, PaymentTransactionDto mockDto)
    {
        //Arrange
        mockDto.Status = status;
        mockDto.ProviderType = nameof(ProviderType.Stripe);

        //Act
        var cancellationRequest = _sut.Map<CancellationRequest>(mockDto);

        //Assert
        cancellationRequest.Status.Should().Be(expectedDomainStatus);
    }

    [Theory]
    [InlineAutoData("stripe", ProviderType.Stripe)]
    [InlineAutoData("Stripe", ProviderType.Stripe)]
    [InlineAutoData("paypal", ProviderType.Paypal)]
    [InlineAutoData("PayPal", ProviderType.Paypal)]
    [InlineAutoData("gocardless", ProviderType.GoCardless)]
    [InlineAutoData("GoCardless", ProviderType.GoCardless)]
    public void GivenProviderTypeIsValidString_WhenAutoMappedToCancellationRequest_ThenProviderTypeMappedAsExpected(
        string providerType, ProviderType expectedProviderType, PaymentTransactionDto mockDto)
    {
        //Arrange
        mockDto.ProviderType = providerType;
        mockDto.Status = nameof(TransactionStatus.Submitted);

        //Act
        var cancellationRequest = _sut.Map<CancellationRequest>(mockDto);

        //Assert
        cancellationRequest.ProviderType.Should().Be(expectedProviderType);
    }

    [Theory, AutoData]
    public void GivenInvalidStatusStringInDto_WhenAutoMappedToCancellationRequest_ThenExceptionIsThrown(PaymentTransactionDto mockDto)
    {
        //Arrange
        mockDto.Status = "some-db-bug-in-status";
        mockDto.ProviderType = nameof(ProviderType.Stripe);

        //Act
        var act = () => _sut.Map<CancellationRequest>(mockDto);

        //Assert
        act.Should().Throw<AutoMapperMappingException>();
    }

    [Theory, AutoData]
    public void GivenInvalidProviderStringInDto_WhenAutoMappedToCancellationRequest_ThenExceptionIsThrown(PaymentTransactionDto mockDto)
    {
        //Arrange
        mockDto.ProviderType = "invalid-provider";
        mockDto.Status = nameof(TransactionStatus.Submitted);

        //Act
        var act = () => _sut.Map<CancellationRequest>(mockDto);

        //Assert
        act.Should().Throw<AutoMapperMappingException>();
    }

    [Fact]
    public void GivenValidDto_WhenAutoMappedToCancellationRequest_ThenAllPropertiesMappedAsExpected()
    {
        //Arrange
        var expectedStatus = TransactionStatus.Submitted;
        var expectedProviderType = ProviderType.Stripe;
        var expectedPaymentRequestId = Guid.NewGuid();
        var expectedPaymentIntentId = "pi_12345abcd";
        var mockDto = new PaymentTransactionDto
        {
            PaymentRequestId = expectedPaymentRequestId,
            Status = "Submitted",
            ProviderType = "Stripe",
            CancellationReason = "should-be-ignored",
            PaymentProviderPaymentTransactionId = expectedPaymentIntentId
        };

        //Act
        var cancellationRequest = _sut.Map<CancellationRequest>(mockDto);

        //Assert
        cancellationRequest.Status.Should().Be(expectedStatus);
        cancellationRequest.PaymentRequestId.Should().Be(expectedPaymentRequestId);
        cancellationRequest.ProviderType.Should().Be(expectedProviderType);
        cancellationRequest.PaymentProviderPaymentTransactionId.Should().Be(expectedPaymentIntentId);
    }
}
