using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Commands;
using PaymentExecutionLambda.CancelLambda.Mappings;
using PaymentExecutionLambda.CancelLambda.Models;

namespace PaymentExecutionLambda.CancelLambdaUnitTests.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingProfile_ShouldBeValid()
    {
        // Assert
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_CancelPaymentMessage_To_ProcessCancelMessageCommand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();

        var cancelPaymentMessage = new CancelPaymentMessage(
            tenantId,
            correlationId,
            new CancelPaymentRequest
            {
                PaymentRequestId = paymentRequestId,
                ProviderType = "Stripe",
                CancellationReason = "Customer request"
            }
        );

        // Act
        var result = _mapper.Map<ProcessCancelMessageCommand>(cancelPaymentMessage);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
        result.CorrelationId.Should().Be(correlationId);
        result.PaymentRequestId.Should().Be(paymentRequestId);
        result.ProviderType.Should().Be("Stripe");
        result.CancellationReason.Should().Be("Customer request");
    }
}

