using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;
using PaymentExecution.StripeExecutionClient.Contracts.Models;
using PayReqDtoEnums = PaymentExecution.PaymentRequestClient.Models.Enums;
using PayReqModels = PaymentExecution.PaymentRequestClient.Models;

namespace PaymentExecution.Domain.UnitTests.Mapping;

public class ServiceToDomainMappingProfileTests
{
    private readonly IMapper _sut;

    public ServiceToDomainMappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ServiceToDomainMappingProfile>());
        config.AssertConfigurationIsValid();
        _sut = config.CreateMapper();
    }

    [Theory]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.created)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.scheduled)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.awaitingexecution)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.executioninprogress)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.executionsuccess)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.cancelled)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.failed)]
    [InlineAutoData(PayReqDtoEnums.RequestStatus.success)]
    public void GivenPayReqPaymentRequestResponseDto_WhenMappedToDomainPaymentRequest_ThenRequestStatusMappedCorrectly(
        PayReqDtoEnums.RequestStatus mockedRequestStatus)
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        payReqPaymentRequestResponseDto.Status = mockedRequestStatus;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.Status.ToString().Should().Be(mockedRequestStatus.ToString());
    }

    [Theory]
    [InlineAutoData(PayReqDtoEnums.ExecutorType.webpay)]
    [InlineAutoData(PayReqDtoEnums.ExecutorType.paymentexecution)]
    public void GivenPayReqPaymentRequestResponseDto_WhenMappedToDomainPaymentRequest_ThenExecutorTypeMappedCorrectly(
        PayReqDtoEnums.ExecutorType mockedExecutor)
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        payReqPaymentRequestResponseDto.Executor = mockedExecutor;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.Executor.ToString().Should().Be(mockedExecutor.ToString());
    }

    [Theory]
    [InlineAutoData(PayReqDtoEnums.ReceivableType.invoice)]
    public void GivenPayReqPaymentRequestResponseDtoWithReceivables_WhenMappedToDomainPaymentRequest_ThenReceivablesMappedAsExpected(
        PayReqDtoEnums.ReceivableType mockedReceivableType)
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var mockedPayReqReceivable = new List<PayReqModels.Receivable>
        {
            new()
            {
                Identifier = Guid.NewGuid(),
                Type = mockedReceivableType
            }
        };
        payReqPaymentRequestResponseDto.Receivables = mockedPayReqReceivable;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.Receivables.Should().NotBeNull();
        domainPaymentRequest.Receivables.Should().BeEquivalentTo(mockedPayReqReceivable);
        domainPaymentRequest.Receivables.Should().HaveCount(1);
        domainPaymentRequest.Receivables![0].Type.ToString().Should().Be(mockedReceivableType.ToString());
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDto_WhenMappedToDomainPaymentRequest_ThenScalarPropertiesMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.PaymentRequestId.Should().Be(payReqPaymentRequestResponseDto.PaymentRequestId);
        domainPaymentRequest.OrganisationId.Should().Be(payReqPaymentRequestResponseDto.OrganisationId);
        domainPaymentRequest.PaymentDateUtc.Should().Be(payReqPaymentRequestResponseDto.PaymentDateUtc);
        domainPaymentRequest.ContactId.Should().Be(payReqPaymentRequestResponseDto.ContactId);
        domainPaymentRequest.Amount.Should().Be(payReqPaymentRequestResponseDto.Amount);
        domainPaymentRequest.Currency.Should().Be(payReqPaymentRequestResponseDto.Currency);
        domainPaymentRequest.PaymentDescription.Should().Be(payReqPaymentRequestResponseDto.PaymentDescription);
        domainPaymentRequest.MerchantReference.Should().Be(payReqPaymentRequestResponseDto.MerchantReference);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoBillingContactDetails_WhenMappedToDomainPaymentRequest_ThenMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var mockPayReqBillingContactDetails = new PayReqModels.BillingContactDetails()
        {
            Email = "testingtime@test.com"
        };
        payReqPaymentRequestResponseDto.BillingContactDetails = mockPayReqBillingContactDetails;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.BillingContactDetails.Should().NotBeNull();
        domainPaymentRequest.BillingContactDetails.Should().BeEquivalentTo(mockPayReqBillingContactDetails);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoSelectedPaymentMethod_WhenMappedToDomainPaymentRequest_ThenMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var mockedPayReqSelectedPaymentMethod = new PayReqModels.SelectedPaymentMethod
        {
            PaymentGatewayId = Guid.NewGuid(),
            PaymentMethodName = "Credit Card",
            SurchargeAmount = 2.50m
        };
        payReqPaymentRequestResponseDto.SelectedPaymentMethod = mockedPayReqSelectedPaymentMethod;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.SelectedPaymentMethod.Should().NotBeNull();
        domainPaymentRequest.SelectedPaymentMethod.Should().BeEquivalentTo(mockedPayReqSelectedPaymentMethod);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoLineItems_WhenMappedToDomainPaymentRequest_ThenMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var mockedPayReqLineItems = new List<PayReqModels.LineItem>
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
        payReqPaymentRequestResponseDto.LineItems = mockedPayReqLineItems;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.LineItems.Should().NotBeNull();
        domainPaymentRequest.LineItems.Should().HaveCount(1);
        domainPaymentRequest.LineItems.Should().BeEquivalentTo(mockedPayReqLineItems);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoSourceContext_WhenMappedToDomainPaymentRequest_ThenMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var mockedPayReqSourceContext = new PayReqModels.SourceContext
        {
            Identifier = Guid.NewGuid(),
            RepeatingTemplateId = Guid.NewGuid(),
            Type = "invoice"
        };
        payReqPaymentRequestResponseDto.SourceContext = mockedPayReqSourceContext;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.SourceContext.Should().NotBeNull();
        domainPaymentRequest.SourceContext.Should().BeEquivalentTo(mockedPayReqSourceContext);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoWithScheduledAndDueDate_WhenMappedToDomainPaymentRequest_ThenPropertiesMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var scheduledDate = DateTime.UtcNow.AddDays(1);
        var dueDate = DateTime.UtcNow.AddDays(30);
        payReqPaymentRequestResponseDto.ScheduledDispatchDateUtc = scheduledDate;
        payReqPaymentRequestResponseDto.DueDateUtc = dueDate;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.ScheduledDispatchDateUtc.Should().Be(scheduledDate);
        domainPaymentRequest.DueDateUtc.Should().Be(dueDate);
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoWithNullOptionalProperties_WhenMappedToDomainPaymentRequest_ThenNullPropertiesAreHandled()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        payReqPaymentRequestResponseDto.ScheduledDispatchDateUtc = null;
        payReqPaymentRequestResponseDto.DueDateUtc = null;
        payReqPaymentRequestResponseDto.Receivables = null;

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.ScheduledDispatchDateUtc.Should().BeNull();
        domainPaymentRequest.DueDateUtc.Should().BeNull();
        domainPaymentRequest.Receivables.Should().BeEmpty(); //Note: Automapper default behaviour for this scenario is to initialise an empty collection
    }

    [Fact]
    public void GivenPayReqPaymentRequestResponseDtoWithMultipleReceivables_WhenMappedToDomainPaymentRequest_ThenAllReceivablesMappedCorrectly()
    {
        // Arrange
        var payReqPaymentRequestResponseDto = CreateValidPayReqPaymentRequestResponseDto();
        var receivable1 = new PayReqModels.Receivable
        {
            Identifier = Guid.NewGuid(),
            Type = PayReqDtoEnums.ReceivableType.invoice
        };
        var receivable2 = new PayReqModels.Receivable
        {
            Identifier = Guid.NewGuid(),
            Type = PayReqDtoEnums.ReceivableType.invoice
        };
        payReqPaymentRequestResponseDto.Receivables = new List<PayReqModels.Receivable> { receivable1, receivable2 };

        // Act
        var domainPaymentRequest = _sut.Map<PaymentRequest>(payReqPaymentRequestResponseDto);

        // Assert
        domainPaymentRequest.Receivables.Should().NotBeNull();
        domainPaymentRequest.Receivables.Should().HaveCount(2);
        domainPaymentRequest.Receivables![0].Should().BeEquivalentTo(receivable1);
        domainPaymentRequest.Receivables[1].Should().BeEquivalentTo(receivable2);
    }

    #region StripeExeSubmitPaymentResponse to SubmittedPayment

    [Fact]
    public void GivenStripeExeSubmitPaymentResponse_WhenMappedToDomainSubmittedPayment_ThenMappedCorrectly()
    {
        // Arrange
        var stripeExeSubmitPaymentResponse = new StripeExeSubmitPaymentResponseDto()
        {
            PaymentIntentId = "pi_32984ihueg",
            ProviderServiceId = Guid.NewGuid(),
            ClientSecret = "pi_32984ihueg_secret_12345",
            TtlInSeconds = 123
        };

        // Act
        var domainSubmitPayment = _sut.Map<SubmittedPayment>(stripeExeSubmitPaymentResponse);

        // Assert
        domainSubmitPayment.Should().BeEquivalentTo(stripeExeSubmitPaymentResponse);
    }

    #endregion

    private static PayReqModels.PaymentRequest CreateValidPayReqPaymentRequestResponseDto()
    {
        return new PayReqModels.PaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            OrganisationId = Guid.NewGuid(),
            PaymentDateUtc = DateTime.UtcNow,
            ScheduledDispatchDateUtc = null,
            ContactId = Guid.NewGuid(),
            Status = PayReqDtoEnums.RequestStatus.created,
            BillingContactDetails = new PayReqModels.BillingContactDetails
            {
                Email = "test@test5real.com"
            },
            Amount = 100.00m,
            Currency = "USD",
            PaymentDescription = "Test Payment",
            SelectedPaymentMethod = new PayReqModels.SelectedPaymentMethod
            {
                PaymentGatewayId = Guid.NewGuid(),
                PaymentMethodName = "Credit Card",
                SurchargeAmount = 1.50m
            },
            LineItems = new List<PayReqModels.LineItem>
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
            SourceContext = new PayReqModels.SourceContext
            {
                Identifier = Guid.NewGuid(),
                RepeatingTemplateId = Guid.NewGuid(),
                Type = "invoice"
            },
            Executor = PayReqDtoEnums.ExecutorType.paymentexecution,
            Receivables = new List<PayReqModels.Receivable>
            {
              new()
              {
                  Identifier = Guid.NewGuid(),
                  Type = PayReqDtoEnums.ReceivableType.invoice
              }
            },
            DueDateUtc = DateTime.UtcNow.AddDays(2),
            MerchantReference = "MERCHANT-REF-001"
        };
    }
}
