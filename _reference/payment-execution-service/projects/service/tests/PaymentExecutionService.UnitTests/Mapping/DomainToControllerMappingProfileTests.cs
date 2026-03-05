using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Mapping;
using PaymentExecutionService.Models.Response;
using Xunit;

namespace PaymentExecutionService.UnitTests.Mapping;

public class DomainToControllerMappingProfileTests
{
    private readonly IMapper _sut;
    public DomainToControllerMappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DomainToControllerMappingProfile>());
        config.AssertConfigurationIsValid();

        _sut = config.CreateMapper();
    }

    [Theory]
    [InlineAutoData(PaymentProviderStatus.Submitted)]
    [InlineAutoData(PaymentProviderStatus.Processing)]
    [InlineAutoData(PaymentProviderStatus.RequiresAction)]
    [InlineAutoData(PaymentProviderStatus.Terminal)]
    public void GivenDomainProviderState_WhenMappedFromDomainToApiResponse_ThenPaymentProviderStatusMappedCorrectly(
        PaymentProviderStatus expectedProviderStatus, ProviderState domainProviderState)
    {
        //Arrange
        domainProviderState.PaymentProviderStatus = expectedProviderStatus;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(domainProviderState);

        //Assert
        apiResponse.PaymentProviderStatus.ToString().Should().Be(expectedProviderStatus.ToString());
    }

    [Theory]
    [InlineAutoData(ProviderType.Stripe)]
    [InlineAutoData(ProviderType.Paypal)]
    [InlineAutoData(ProviderType.GoCardless)]
    public void GivenDomainProviderState_WhenMappedFromDomainToApiResponse_ThenProviderTypeMappedCorrectly(
        ProviderType expectedProviderType, ProviderState domainProviderState)
    {
        //Arrange
        domainProviderState.ProviderType = expectedProviderType;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(domainProviderState);

        //Assert
        apiResponse.ProviderType.ToString().Should().Be(expectedProviderType.ToString());
    }

    [Theory, AutoData]
    public void GivenDomainProviderState_WhenMappedFromDomainToApiResponse_ThenNonEnumPropertiesAreMappedAsExpected(
        ProviderState mockedProviderState)
    {
        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PaymentProviderPaymentTransactionId.Should().Be(mockedProviderState.PaymentProviderPaymentTransactionId);
        apiResponse.LastPaymentErrorCode.Should().Be(mockedProviderState.LastPaymentErrorCode);
        apiResponse.PendingStatusDetails.Should().BeEquivalentTo(mockedProviderState.PendingStatusDetails);
    }

    [Theory, AutoData]
    [InlineAutoData("some-error-code")]
    [InlineAutoData(null)]
    public void GivenDomainProviderState_WhenMappedFromDomainToApiResponse_ThenLastPaymentErrorMappedAsExpected(
        string? lastPaymentErrorCode, ProviderState providerState)
    {
        //Arrange
        providerState.LastPaymentErrorCode = lastPaymentErrorCode;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(providerState);

        //Assert
        apiResponse.LastPaymentErrorCode.Should().Be(lastPaymentErrorCode);
    }


    [Theory, AutoData]
    public void GivenDomainProviderStateWithNullPendingStatusDetails_WhenMappedFromDomainToApiResponse_ThenPendingStatusDetailsIsNull(
        ProviderState mockedProviderState)
    {
        //Arrange
        mockedProviderState.PendingStatusDetails = null;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().BeNull();
    }

    [Theory, AutoData]
    [InlineAutoData(RequiresActionType.BankTransferInstructions)]
    [InlineAutoData(RequiresActionType.RedirectToUrl)]
    [InlineAutoData(RequiresActionType.MicrodepositVerification)]
    [InlineAutoData(RequiresActionType.PayToAuthorization)]
    [InlineAutoData(RequiresActionType.Unknown)]
    public void GivenDomainProviderStateWithPendingStatusDetails_WhenMappedFromDomainToApiResponse_ThenRequiresActionTypeMappedAsExpected(
        RequiresActionType requiresActionType, ProviderState mockedProviderState, PendingStatusDetails mockedPendingStatusDetails)
    {
        //Arrange
        mockedPendingStatusDetails.RequiresActionType = requiresActionType;
        mockedProviderState.PendingStatusDetails = mockedPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.RequiresActionType.ToString().Should().Be(requiresActionType.ToString());
    }


    [Theory, AutoData]
    [InlineAutoData(PaymentMethodType.Card)]
    [InlineAutoData(PaymentMethodType.Link)]
    [InlineAutoData(PaymentMethodType.CustomerBalance)]
    [InlineAutoData(PaymentMethodType.AfterPay)]
    [InlineAutoData(PaymentMethodType.AuBecsDebit)]
    [InlineAutoData(PaymentMethodType.BacsDebit)]
    [InlineAutoData(PaymentMethodType.Klarna)]
    [InlineAutoData(PaymentMethodType.UsBankAccount)]
    [InlineAutoData(PaymentMethodType.Zip)]
    [InlineAutoData(PaymentMethodType.PayTo)]
    [InlineAutoData(PaymentMethodType.PayByBank)]
    [InlineAutoData(PaymentMethodType.ApplePay)]
    [InlineAutoData(PaymentMethodType.GooglePay)]
    public void GivenDomainProviderStateWithPendingStatusDetails_WhenMappedFromDomainToApiResponse_ThenPaymentMethodTypeMappedAsExpected(
        PaymentMethodType mockedDomainPaymentMethodType, ProviderState mockedProviderState, PendingStatusDetails mockedPendingStatusDetails)
    {
        //Arrange
        mockedPendingStatusDetails.PaymentMethodType = mockedDomainPaymentMethodType;
        mockedProviderState.PendingStatusDetails = mockedPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.PaymentMethodType.ToString().Should().Be(mockedDomainPaymentMethodType.ToString());
    }

    [Theory, AutoData]
    public void GivenDomainProviderStateWithBankTransferInstructions_WhenMappedFromDomainToApiResponse_ThenPendingStatusDetailsMappedAsExpected(
        ProviderState mockedProviderState)
    {
        //Arrange
        var mockPendingStatusDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            PaymentMethodType = PaymentMethodType.UsBankAccount,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = 1000,
                PaymentProviderPaymentCurrency = "USD",
                BankTransferCurrency = "USD",
                AmountRemaining = 1000,
                Reference = "REF123",
                FinancialAddresses = new FinancialAddresses()
                {
                    Aba = new Aba()
                    {
                        AccountNumber = "123456789",
                        BankName = "Test Bank",
                        RoutingNumber = "987654321"
                    },
                    Swift = new Swift() { SwiftCode = "TESTUS33" }
                }
            }
        };
        mockedProviderState.PendingStatusDetails = mockPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.RedirectToUrl.Should().BeNull();
        apiResponse.PendingStatusDetails.MicroDepositVerification.Should().BeNull();
        apiResponse.PendingStatusDetails.Should().BeEquivalentTo(mockPendingStatusDetails);
    }

    [Theory, AutoData]
    public void GivenDomainProviderStateWithRedirectToUrl_WhenMappedFromDomainToApiResponse_ThenPendingStatusDetailsMappedAsExpected(
        ProviderState mockedProviderState)
    {
        //Arrange
        var mockPendingStatusDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            PaymentMethodType = PaymentMethodType.UsBankAccount,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = "https://www.getyoselfredirected/redirect"
            }
        };
        mockedProviderState.PendingStatusDetails = mockPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.BankTransferInstructions.Should().BeNull();
        apiResponse.PendingStatusDetails.MicroDepositVerification.Should().BeNull();
        apiResponse.PendingStatusDetails.Should().BeEquivalentTo(mockPendingStatusDetails);
    }

    [Theory, AutoData]
    public void GivenDomainProviderStateWithMicrodepositVerification_WhenMappedFromDomainToApiResponse_ThenPendingStatusDetailsMappedAsExpected(
        ProviderState mockedProviderState)
    {
        //Arrange
        var mockPendingStatusDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            PaymentMethodType = PaymentMethodType.UsBankAccount,
            HasActionValue = true,
            MicroDepositVerification = new MicroDepositVerification()
            {
                HostedVerificationUrl = "https://www.verifydemicrodeposits/verify",
                BankAccountLast4 = "6789"
            }
        };
        mockedProviderState.PendingStatusDetails = mockPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.BankTransferInstructions.Should().BeNull();
        apiResponse.PendingStatusDetails.RedirectToUrl.Should().BeNull();
        apiResponse.PendingStatusDetails.Should().BeEquivalentTo(mockPendingStatusDetails);
    }

    [Theory, AutoData]
    public void GivenDomainProviderStateWithPayToAuthorization_WhenMappedFromDomainToApiResponse_ThenPendingStatusDetailsMappedAsExpected(
        ProviderState mockedProviderState)
    {
        //Arrange
        var mockPendingStatusDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.PayToAuthorization,
            PaymentMethodType = PaymentMethodType.PayTo,
            HasActionValue = false
        };
        mockedProviderState.PendingStatusDetails = mockPendingStatusDetails;

        //Act
        var apiResponse = _sut.Map<GetProviderStateResponse>(mockedProviderState);

        //Assert
        apiResponse.PendingStatusDetails.Should().NotBeNull();
        apiResponse.PendingStatusDetails.BankTransferInstructions.Should().BeNull();
        apiResponse.PendingStatusDetails.RedirectToUrl.Should().BeNull();
        apiResponse.PendingStatusDetails.MicroDepositVerification.Should().BeNull();
        apiResponse.PendingStatusDetails.Should().BeEquivalentTo(mockPendingStatusDetails);
    }
}

