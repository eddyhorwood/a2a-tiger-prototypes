using AutoFixture;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.UnitTests.Mapping;
public class DomainToServiceMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly IFixture _fixture = new Fixture();

    public DomainToServiceMappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg => { cfg.AddProfile(new DomainToServiceMappingProfile()); });

        _mapper = configuration.CreateMapper();

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void GivenCompleteMessageEvent_WhenAutoMappedWithSuccessPaymentRequest_ThenMapped()
    {
        // Arrange
        var completeMessage = _fixture.Create<CompleteMessage>();


        // Act
        var successRequest = _mapper.Map<SuccessPaymentRequest>(completeMessage);

        // Assert
        successRequest.PaymentCompletionDateTime.Should().Be(completeMessage.EventCreatedDateTime);
        successRequest.Fee.Should().Be(completeMessage.Fee);
        successRequest.FeeCurrency.Should().Be(completeMessage.FeeCurrency);
        successRequest.PaymentProviderPaymentTransactionId.Should().Be(completeMessage.PaymentProviderPaymentTransactionId);
        successRequest.PaymentProviderPaymentReferenceId.Should().Be(completeMessage.PaymentProviderPaymentReferenceId);
        successRequest.PaymentProviderLastUpdatedAt.Should().Be(completeMessage.PaymentProviderLastUpdatedAt);

    }

    [Fact]
    public void GivenCompleteMessageEvent_WhenAutoMappedWithCancelPaymentRequest_ThenMapped()
    {
        // Arrange
        var completeMessage = _fixture.Create<CompleteMessage>();


        // Act
        var cancelRequest = _mapper.Map<PaymentRequestClient.Models.Requests.CancelPaymentRequest>(completeMessage);

        // Assert
        cancelRequest.PaymentProviderPaymentTransactionId.Should().Be(completeMessage.PaymentProviderPaymentTransactionId);
        cancelRequest.PaymentProviderLastUpdatedAt.Should().Be(completeMessage.PaymentProviderLastUpdatedAt);
        cancelRequest.CancellationReason.Should().Be(completeMessage.CancellationReason);
    }

    [Fact]
    public void GivenPaymentTransactionDto_WhenAutoMapped_ThenMappedToGetPaymentTransactionQueryResponse()
    {
        // Arrange
        var completeMessage = _fixture.Create<CompleteMessage>();


        // Act
        var successRequest = _mapper.Map<FailurePaymentRequest>(completeMessage);

        // Assert
        successRequest.PaymentCompletionDateTime.Should().Be(completeMessage.EventCreatedDateTime);
        successRequest.Fee.Should().Be(completeMessage.Fee);
        successRequest.FeeCurrency.Should().Be(completeMessage.FeeCurrency);
        successRequest.PaymentProviderPaymentTransactionId.Should().Be(completeMessage.PaymentProviderPaymentTransactionId);
        successRequest.PaymentProviderPaymentReferenceId.Should().Be(completeMessage.PaymentProviderPaymentReferenceId);
        successRequest.FailureDetails.Should().Be(completeMessage.FailureDetails);
    }

    [Fact]
    public void GivenDomainCancelPaymentRequest_WhenMappedToStripeExecutionCancelPaymentRequest_ThenMapsAsExpected()
    {
        // Arrange
        var domainCancelRequest = _fixture.Create<Models.CancelPaymentRequest>();

        // Act
        var stripeExecutionCancelRequest = _mapper.Map<StripeExeCancelPaymentRequestDto>(domainCancelRequest);

        // Assert
        stripeExecutionCancelRequest.PaymentRequestId.Should().Be(domainCancelRequest.PaymentRequestId);
        stripeExecutionCancelRequest.CancellationReason.Should().Be(domainCancelRequest.CancellationReason);
        stripeExecutionCancelRequest.TenantId.Should().Be(domainCancelRequest.TenantId);
        stripeExecutionCancelRequest.CorrelationId.Should().Be(domainCancelRequest.CorrelationId);
    }

    #region Domain PaymentRequest to StripeExePaymentRequestDto Tests

    [Fact]
    public void GivenDomainPaymentRequest_WhenMappedToStripeExePaymentRequestDto_ThenScalarPropertiesMappedCorrectly()
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.PaymentRequestId.Should().Be(domainPaymentRequest.PaymentRequestId);
        stripeExePaymentRequest.OrganisationId.Should().Be(domainPaymentRequest.OrganisationId);
        stripeExePaymentRequest.Amount.Should().Be(domainPaymentRequest.Amount);
        stripeExePaymentRequest.Currency.Should().Be(domainPaymentRequest.Currency);
        stripeExePaymentRequest.PaymentDescription.Should().Be(domainPaymentRequest.PaymentDescription);
        stripeExePaymentRequest.Executor.Should().Be(domainPaymentRequest.Executor.ToString());
        stripeExePaymentRequest.MerchantReference.Should().Be(domainPaymentRequest.MerchantReference);
    }

    [Theory]
    [InlineAutoData(ExecutorType.webpay)]
    [InlineAutoData(ExecutorType.paymentexecution)]
    public void GivenDomainPaymentRequestExecutor_WhenMappedToStripeExePaymentRequestDto_ThenMappedCorrectly(
        ExecutorType mockedExecutor)
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();
        domainPaymentRequest.Executor = mockedExecutor;

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.Executor.Should().Be(mockedExecutor.ToString());
    }

    [Fact]
    public void GivenDomainPaymentRequestBillingContactDetails_WhenMappedToStripeExePaymentRequestDto_ThenMappedCorrectly()
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();
        var mockedDomainContactBillingDetails = new BillingContactDetails() { Email = "testthisguy@test.com" };
        domainPaymentRequest.BillingContactDetails = mockedDomainContactBillingDetails;

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.BillingContactDetails.Should().NotBeNull();
        stripeExePaymentRequest.BillingContactDetails.Should().BeEquivalentTo(mockedDomainContactBillingDetails);
    }

    [Fact]
    public void GivenDomainPaymentRequestSelectedPaymentMethod_WhenMappedToStripeExePaymentRequestDto_ThenMappedCorrectly()
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();
        var mockedDomainSelectedPaymentMethod = new SelectedPaymentMethod()
        {
            PaymentGatewayId = Guid.NewGuid(),
            PaymentMethodName = "some-gateway",
            SurchargeAmount = 2
        };
        domainPaymentRequest.SelectedPaymentMethod = mockedDomainSelectedPaymentMethod;

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.SelectedPaymentMethod.Should().NotBeNull();
        stripeExePaymentRequest.SelectedPaymentMethod.Should().BeEquivalentTo(mockedDomainSelectedPaymentMethod);
    }

    [Fact]
    public void GivenDomainPaymentRequestSourceContext_WhenMappedToStripeExePaymentRequestDto_ThenMappedCorrectly()
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();
        var mockedDomainSourceContext = new SourceContext()
        {
            Identifier = Guid.NewGuid(),
            RepeatingTemplateId = Guid.NewGuid(),
            Type = "some-type"
        };
        domainPaymentRequest.SourceContext = mockedDomainSourceContext;

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.SourceContext.Should().NotBeNull();
        stripeExePaymentRequest.SourceContext.Should().BeEquivalentTo(mockedDomainSourceContext);
    }

    [Fact]
    public void GivenDomainPaymentRequestLineItems_WhenMappedToStripeExePaymentRequest_ThenMappedCorrectly()
    {
        // Arrange
        var domainPaymentRequest = CreateValidDomainPaymentRequest();
        var mockedDomainLineItem = new List<LineItem>
        {
            new()
            {
                Reference = "REF001",
                Description = "Test Item",
                UnitCost = 100.50m,
                Quantity = 2,
                TaxAmount = 20.10m,
                DiscountAmount = 5.00m
            }
        };
        domainPaymentRequest.LineItems = mockedDomainLineItem;

        // Act
        var stripeExePaymentRequest = _mapper.Map<StripeExePaymentRequestDto>(domainPaymentRequest);

        // Assert
        stripeExePaymentRequest.LineItems.Should().NotBeNull();
        stripeExePaymentRequest.LineItems.Should().HaveCount(1);
        stripeExePaymentRequest.LineItems.Should().BeEquivalentTo(mockedDomainLineItem);
    }

    #endregion

    private static PaymentRequest CreateValidDomainPaymentRequest()
    {
        return new PaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            OrganisationId = Guid.NewGuid(),
            PaymentDateUtc = DateTime.UtcNow,
            ScheduledDispatchDateUtc = null,
            ContactId = Guid.NewGuid(),
            Status = RequestStatus.created,
            BillingContactDetails = new BillingContactDetails
            {
                Email = "test@example.com"
            },
            Amount = 100.00m,
            Currency = "USD",
            PaymentDescription = "Test Payment",
            SelectedPaymentMethod = new SelectedPaymentMethod
            {
                PaymentGatewayId = Guid.NewGuid(),
                PaymentMethodName = "Credit Card",
                SurchargeAmount = 1.50m
            },
            LineItems = new List<LineItem>
            {
                new()
                {
                    Reference = "REF001",
                    Description = "Test Item",
                    UnitCost = 100.00m,
                    Quantity = 1,
                    TaxAmount = 0.00m,
                    DiscountAmount = 0.00m
                }
            },
            SourceContext = new SourceContext
            {
                Identifier = Guid.NewGuid(),
                RepeatingTemplateId = Guid.NewGuid(),
                Type = "invoice"
            },
            Executor = ExecutorType.paymentexecution,
            Receivables = new List<Receivable>
            {
                new()
                {
                    Identifier = Guid.NewGuid(),
                    Type = ReceivableType.invoice
                }
            },
            DueDateUtc = DateTime.UtcNow.AddDays(2),
            MerchantReference = "MERCHANT-REF-001"
        };
    }
}

