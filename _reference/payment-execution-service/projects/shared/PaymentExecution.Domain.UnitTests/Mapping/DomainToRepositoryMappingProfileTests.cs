using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Queries;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Domain.UnitTests.Mapping;
public class DomainToRepositoryMappingProfileTests
{
    private readonly IMapper _mapper;

    public DomainToRepositoryMappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg => { cfg.AddProfile(new DomainToRepositoryMappingProfile()); });

        _mapper = configuration.CreateMapper();

        // Ensure the configuration is valid
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void GivenPaymentTransactionDto_WhenAutoMapped_ThenMappedToGetPaymentTransactionQueryResponse()
    {
        // Arrange
        var paymentTransactionDto = new PaymentTransactionDto
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderServiceId = Guid.NewGuid(),
            ProviderType = "Stripe",
            PaymentTransactionId = Guid.NewGuid(),
            Status = "submitted",
            Fee = 10,
            FeeCurrency = "USD",
            PaymentProviderPaymentReferenceId = "ch_1234",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            CancellationReason = "test string"
        };


        // Act
        var getPaymentTransactionQueryResponse = _mapper.Map<GetPaymentTransactionQueryResponse>(paymentTransactionDto);

        // Assert
        getPaymentTransactionQueryResponse.PaymentTransactionId.Should().Be(paymentTransactionDto.PaymentTransactionId);
        getPaymentTransactionQueryResponse.PaymentRequestId.Should().Be(paymentTransactionDto.PaymentRequestId);
        getPaymentTransactionQueryResponse.ProviderServiceId.Should().Be(paymentTransactionDto.ProviderServiceId);
        getPaymentTransactionQueryResponse.ProviderType.Should().Be(paymentTransactionDto.ProviderType);
        getPaymentTransactionQueryResponse.Status.Should().Be(paymentTransactionDto.Status);
        getPaymentTransactionQueryResponse.Fee.Should().Be(paymentTransactionDto.Fee);
        getPaymentTransactionQueryResponse.FeeCurrency.Should().Be(paymentTransactionDto.FeeCurrency);
        getPaymentTransactionQueryResponse.PaymentProviderPaymentReferenceId.Should().Be(paymentTransactionDto.PaymentProviderPaymentReferenceId);
        getPaymentTransactionQueryResponse.CreatedUtc.Should().Be(paymentTransactionDto.CreatedUtc);
        getPaymentTransactionQueryResponse.UpdatedUtc.Should().Be(paymentTransactionDto.UpdatedUtc);
        getPaymentTransactionQueryResponse.CancellationReason.Should().Be(paymentTransactionDto.CancellationReason);
    }

    [Fact]
    public void GivenCompleteBodyMessage_WhenAutoMapped_ThenMappedToUpdateCancelledPaymentTransactionDto()
    {
        //Arrange

        var completeMessage = new CompleteMessage
        {
            PaymentRequestId = Guid.NewGuid(),
            Status = "Cancelled",
            EventCreatedDateTime = DateTime.UtcNow,
            PaymentProviderPaymentReferenceId = "pi_5678",
            ProviderServiceId = Guid.NewGuid(),
            XeroCorrelationId = Guid.NewGuid().ToString(),
            XeroTenantId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            ReceiptHandle = Guid.NewGuid().ToString(),
            CancellationReason = "Abandoned"
        };

        //Act
        var updateCancelledPaymentTransactionDto = _mapper.Map<UpdateCancelledPaymentTransactionDto>(completeMessage);

        //Assert
        updateCancelledPaymentTransactionDto.PaymentRequestId.Should().Be(completeMessage.PaymentRequestId);
        updateCancelledPaymentTransactionDto.ProviderServiceId.Should().Be(completeMessage.ProviderServiceId);
        updateCancelledPaymentTransactionDto.Status.Should().Be(completeMessage.Status);
        updateCancelledPaymentTransactionDto.EventCreatedDateTimeUtc.Should().Be(completeMessage.EventCreatedDateTime);
        updateCancelledPaymentTransactionDto.CancellationReason.Should().Be(completeMessage.CancellationReason);
    }

}

