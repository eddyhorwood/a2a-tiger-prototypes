using System;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Mapping;
using PaymentExecutionService.Models;
using Xunit;

namespace PaymentExecutionService.UnitTests.Mapping;

public class ControllerToDomainMappingProfileTests
{
    private readonly IMapper _sut;
    public ControllerToDomainMappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ControllerToDomainMappingProfile>());
        _sut = config.CreateMapper();
    }

    [Fact]
    public void GivenValidCompletePaymentTransactionRequest_WhenMapCalled_ThenMappingOccursSuccessfully()
    {
        var request = new CompletePaymentTransactionRequest
        {
            Fee = 100,
            FeeCurrency = "USD",
            ProviderType = ProviderType.Stripe,
            PaymentProviderPaymentTransactionId = "123456",
            Status = TerminalStatus.Succeeded,
            EventCreatedDateTime = DateTime.UtcNow,
            CancellationReason = "Abandoned"
        };

        // Act
        var queueMessage = _sut.Map<ExecutionQueueMessage>(request);

        // Assert
        Assert.Equal(request.Fee, queueMessage.Fee);
        Assert.Equal(request.FeeCurrency, queueMessage.FeeCurrency);
        Assert.Equal(request.ProviderType.ToString(), queueMessage.ProviderType);
        Assert.Equal(request.PaymentProviderPaymentTransactionId, queueMessage.PaymentProviderPaymentTransactionId);
        Assert.Equal(request.PaymentProviderPaymentReferenceId, queueMessage.PaymentProviderPaymentReferenceId);
        Assert.Equal(request.Status.ToString(), queueMessage.Status);
        Assert.Equal(request.CancellationReason, queueMessage.CancellationReason);
    }

    [Theory]
    [InlineAutoData(0.1)]
    [InlineAutoData(0.49)]
    [InlineAutoData(0.5)]
    [InlineAutoData(0.99)]
    [InlineAutoData(4.1)]
    [InlineAutoData(4.9)]
    [InlineAutoData(12)]
    public void GivenFeeIsPresentInCompletePaymentTransactionRequest_WhenMapCalled_ThenFeeIsMappedAsDecimalWithoutRounding(decimal fee, CompletePaymentTransactionRequest request)
    {
        request.Fee = fee;

        var mappedResult = _sut.Map<ExecutionQueueMessage>(request);

        Assert.Equal(fee, mappedResult.Fee);
    }

    [Fact]
    public void GivenValidSubmitStripeRequest_WhenMapCalled_ThenMappingOccursSuccessfully()
    {
        var request = new SubmitStripeRequest()
        {
            PaymentRequestId = Guid.NewGuid(),
            PaymentMethodId = "pmid_12345",
            PaymentMethodsMadeAvailable = ["card"]
        };

        // Act
        var command = _sut.Map<SubmitStripePaymentCommand>(request);

        // Assert
        Assert.Equal(request.PaymentRequestId, command.PaymentRequestId);
        Assert.Equal(request.PaymentMethodId, command.PaymentMethodId);
        Assert.Equal(request.PaymentMethodsMadeAvailable, command.PaymentMethodsMadeAvailable);
    }

    [Fact]
    public void GivenValidSubmitStripeRequest_WhenMapCalled_ThenIgnoredPropertiesAreNotSetByMapper()
    {
        // Arrange
        var request = new SubmitStripeRequest()
        {
            PaymentRequestId = Guid.NewGuid(),
            PaymentMethodId = "pmid_12345",
            PaymentMethodsMadeAvailable = ["card"]
        };

        // Act
        var command = _sut.Map<SubmitStripePaymentCommand>(request);

        // Assert
        // Verify ignored properties are not set by mapper (will be null)
        // These properties should be set manually from HTTP headers in the controller
        Assert.Null(command.XeroCorrelationId);
        Assert.Null(command.XeroTenantId);
    }

    [Fact]
    public void GivenValidSubmitStripeRequest_WhenMapCalled_ThenAllPropertiesAreSet()
    {
        // Arrange
        var request = new SubmitStripeRequest()
        {
            PaymentRequestId = Guid.NewGuid(),
            PaymentMethodId = "pmid_12345",
            PaymentMethodsMadeAvailable = ["card"]
        };

        // Act
        var command = _sut.Map<SubmitStripePaymentCommand>(request);

        // Assert
        Assert.Equal(request.PaymentRequestId, command.PaymentRequestId);
        Assert.Equal(request.PaymentMethodId, command.PaymentMethodId);
        Assert.Equal(request.PaymentMethodsMadeAvailable, command.PaymentMethodsMadeAvailable);
    }

    [Fact]
    public void GivenRequestCancelPayloadWithLongCancellationReason_WhenMappingRequestCancelPayloadAndRequestCancelCommand_ThenCancellationReasonIsTruncated()
    {
        var maxCancellationReasonLength = 125;
        var request = new RequestCancelPayload
        {
            CancellationReason = new string('a', maxCancellationReasonLength + 1)
        };

        var command = _sut.Map<RequestCancelCommand>(request);

        command.CancellationReason.Length.Should().Be(maxCancellationReasonLength);
    }

    [Fact]
    public void GivenRequestCancelPayloadWithShortCancellationReason_WhenMappingRequestCancelPayloadAndRequestCancelCommand_ThenOriginalCancellationReasonRemains()
    {
        //Arrange
        var mockedCancellationReason = "Short and sweet";
        var request = new RequestCancelPayload
        {
            CancellationReason = mockedCancellationReason
        };

        //Act
        var command = _sut.Map<RequestCancelCommand>(request);

        //Assert
        command.CancellationReason.Should().Be(mockedCancellationReason);
    }

    [Fact]
    public void GivenRequestCancelPayload_WhenMappingRequestCancelPayloadAndRequestCancelCommand_ThenOtherPropertiesAreIgnored()
    {
        //Arrange
        var request = new RequestCancelPayload
        {
            CancellationReason = "Any reason"
        };

        //Act
        var command = _sut.Map<RequestCancelCommand>(request);

        //Assert - These values are set in the controller rather than this mapping
        command.PaymentRequestId.Should().Be(Guid.Empty);
        command.XeroCorrelationId.Should().Be(Guid.Empty);
        command.XeroTenantId.Should().Be(Guid.Empty);
    }
}
