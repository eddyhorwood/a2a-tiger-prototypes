using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.UnitTests.Mapping;

public class DomainToDomainMappingProfileTests
{
    private readonly IMapper _mapper;

    public DomainToDomainMappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DomainToDomainMappingProfile>();
        });
        _mapper = configuration.CreateMapper();

    }

    [Fact]
    public void GivenAutoMapper_WhenMappingCompleteMessageBodyToCompleteMessage_ThenMappingOccursAsExpected()
    {
        var paymentRequestId = Guid.NewGuid();
        var providerServiceId = Guid.NewGuid();
        var terminalStatus = TerminalStatus.Succeeded.ToString();

        var completeMessageBody = new CompleteMessageBody()
        {
            PaymentRequestId = paymentRequestId,
            ProviderServiceId = providerServiceId,
            Status = terminalStatus
        };

        var result = _mapper.Map<CompleteMessage>(completeMessageBody);

        Assert.Equal(paymentRequestId, result.PaymentRequestId);
        Assert.Equal(providerServiceId, result.ProviderServiceId);
        Assert.Equal(terminalStatus, result.Status);
        Assert.Null(result.MessageId);
        Assert.Null(result.ReceiptHandle);
        Assert.Null(result.XeroCorrelationId);
    }

    [Fact]
    public void GivenAutoMapper_WhenMappingProcessCancelMessageCommandToCancelPaymentRequest_ThenMappingOccursAsExpected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var providerType = "Stripe";
        var cancellationReason = "User cancelled payment";

        var command = new ProcessCancelMessageCommand
        {
            TenantId = tenantId,
            CorrelationId = correlationId,
            PaymentRequestId = paymentRequestId,
            ProviderType = providerType,
            CancellationReason = cancellationReason
        };

        // Act
        var result = _mapper.Map<CancelPaymentRequest>(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(paymentRequestId, result.PaymentRequestId);
        Assert.Equal(cancellationReason, result.CancellationReason);
    }

    [Fact]
    public void GivenMappingFromSynchronousCancellationCommandToCancelPaymentRequest_WhenMap_ThenMapsAsExpected()
    {
        // Arrange
        var syncCancelCommand = new SynchronousCancellationCommand()
        {
            PaymentRequestId = Guid.NewGuid(),
            TenantId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            CancellationReason = "User cancelled payment"
        };

        // Act
        var mappedResult = _mapper.Map<CancelPaymentRequest>(syncCancelCommand);

        // Assert
        mappedResult.PaymentRequestId.Should().Be(syncCancelCommand.PaymentRequestId);
        mappedResult.CorrelationId.Should().Be(syncCancelCommand.CorrelationId);
        mappedResult.TenantId.Should().Be(syncCancelCommand.TenantId);
        mappedResult.CancellationReason.Should().Be(syncCancelCommand.CancellationReason);
    }

    [Fact]
    public void GivenProcessCancelMessageCommand_WhenMappingToGetProviderStateRequest_ThenMapsAllPropertiesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var providerType = "Stripe";
        var cancellationReason = "User cancelled payment";

        var command = new ProcessCancelMessageCommand
        {
            TenantId = tenantId,
            CorrelationId = correlationId,
            PaymentRequestId = paymentRequestId,
            ProviderType = providerType,
            CancellationReason = cancellationReason
        };

        // Act
        var result = _mapper.Map<GetProviderStateRequest>(command);

        // Assert
        result.Should().NotBeNull();
        result.PaymentRequestId.Should().Be(paymentRequestId);
        result.CorrelationId.Should().Be(correlationId);
        result.TenantId.Should().Be(tenantId);
        result.ProviderType.Should().Be(providerType);
    }
}
