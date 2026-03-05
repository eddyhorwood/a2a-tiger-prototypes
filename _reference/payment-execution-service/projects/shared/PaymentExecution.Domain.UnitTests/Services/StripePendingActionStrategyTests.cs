using FluentAssertions;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service.Strategies;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.UnitTests.Services;

public class MicrodepositVerificationStrategyTests
{
    private readonly MicrodepositVerificationStrategy _strategyUnderTests = new();

    [Fact]
    public void GivenMicrodepositVerificationActionType_WhenActionType_ThenReturnsExpectedActionType()
    {
        // Arrange
        var strategyActionType = "verify_with_microdeposits";

        // Act
        var actualActionType = _strategyUnderTests.ActionType;

        // Assert
        actualActionType.Should().Be(strategyActionType);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasVerifyWithMicrodepositsValue_WhenMapMicrodeposit_ThenReturnsExpectedPendingStatusDetails(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedHostedVerificationUrl = "https://megafake.verification.url";
        var mockedDtoPaymentMethod = new StripeExePaymentMethodDto()
        {
            Type = "Klarna",
            UsBankAccount = new StripeExeUsBankAccountDto()
            {
                Last4 = "6789"
            }
        };
        mockPaymentIntentDto.PaymentMethod = mockedDtoPaymentMethod;
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "verify_with_microdeposits",
            VerifyWithMicrodeposits = new StripeExeVerifyWithMicrodepositsDto()
            {
                HostedVerificationUrl = mockedHostedVerificationUrl
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTests.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.MicrodepositVerification,
            HasActionValue = true,
            MicroDepositVerification = new MicroDepositVerification()
            {
                HostedVerificationUrl = mockedHostedVerificationUrl,
                BankAccountLast4 = mockPaymentIntentDto.PaymentMethod.UsBankAccount.Last4
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasNullUsBankAccount_WhenMapMicrodeposit_ThenReturnsNullBankAccountLast4(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedHostedVerificationUrl = "https://megafake.verification.url";
        var mockedDtoPaymentMethod = new StripeExePaymentMethodDto() { Type = "Klarna", UsBankAccount = null, };
        mockPaymentIntentDto.PaymentMethod = mockedDtoPaymentMethod;
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "verify_with_microdeposits",
            VerifyWithMicrodeposits = new StripeExeVerifyWithMicrodepositsDto()
            {
                HostedVerificationUrl = mockedHostedVerificationUrl,
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTests.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        pendingStatusDetail.MicroDepositVerification.Should().NotBeNull();
        pendingStatusDetail.MicroDepositVerification.BankAccountLast4.Should().BeNull();
    }

    [Theory, ContractsAutoData]
    public void
        GivenDtoHasNullMicrodepositVerification_WhenMapMicrodeposit_ThenReturnsPendingStatusDetailsWithNullMicroDepositVerification(
            StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "verify_with_microdeposits",
            VerifyWithMicrodeposits = null
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTests.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.MicrodepositVerification,
            HasActionValue = false,
            MicroDepositVerification = null
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }
}

public class PayToAuthorizationStrategyTests
{
    private readonly PayToAuthorizationStrategy _strategyUnderTest = new();

    [Fact]
    public void GivenPayToAuthorizationActionType_WhenActionType_ThenReturnsExpectedActionType()
    {
        // Arrange
        var expectedActionType = "payto_await_authorization";

        // Act
        var strategyActionType = _strategyUnderTest.ActionType;

        // Assert
        strategyActionType.Should().Be(expectedActionType);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasNextActionTypePaytoAuthorization_WhenMapPayToAuthorization_ThenReturnsExpectedPendingStatusDetails(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "payto_await_authorization",
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.PayToAuthorization,
            HasActionValue = false,
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }
}

public class RedirectToUrlStrategyTests
{
    private readonly RedirectToUrlStrategy _strategyUnderTest = new();
    [Fact]
    public void GivenRedirectToUrlActionType_WhenActionType_ThenReturnsExpectedActionType()
    {
        // Arrange
        var expectedActionType = "redirect_to_url";

        // Act
        var strategyActionType = _strategyUnderTest.ActionType;

        // Assert
        strategyActionType.Should().Be(expectedActionType);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasRedirectToUrlValue_WhenMapRedirectToUrl_ThenReturnsExpectedPendingStatusDetails(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedRedirectUrl = "https://megafake.redirect.url";
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "redirect_to_url",
            RedirectToUrl = new StripeExeRedirectToUrlDto()
            {
                Url = mockedRedirectUrl
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.RedirectToUrl,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = mockedRedirectUrl
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasNullRedirectToUrl_WhenMapRedirectToUrl_ThenReturnsPendingStatusDetailsWithNullRedirectUrl(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "redirect_to_url",
            RedirectToUrl = null
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.RedirectToUrl,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = null
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasRedirectToUrlWithNullUrl_WhenMapRedirectToUrl_ThenReturnsPendingStatusDetailsWithNullRedirectUrl(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "redirect_to_url",
            RedirectToUrl = new StripeExeRedirectToUrlDto()
            {
                Url = null
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.RedirectToUrl,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = null
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }
}

public class BankTransferStrategyTests
{
    private readonly BankTransferStrategy _strategyUnderTest = new();

    [Fact]
    public void GivenBankTransferActionType_WhenActionType_ThenReturnsExpectedActionType()
    {
        // Arrange
        var expectedActionType = "display_bank_transfer_instructions";

        // Act
        var strategyActionType = _strategyUnderTest.ActionType;

        // Assert
        strategyActionType.Should().Be(expectedActionType);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasDisplayBankTransferInstructionsValueWithAbaAndSwift_WhenMapBankTransferStrategy_ThenReturnsExpectedPendingStatusDetails(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedAmountRemaining = 5000L;
        var mockedCurrency = "aud";
        var mockedReference = "REF123456";
        var mockedFinancialAddresses = new List<StripeExeFinancialAddressDto>()
        {
            new()
            {
                Type = "aba",
                Aba = new StripeExeAbaAddressDto()
                {
                    AccountNumber = "123456789",
                    BankName = "Bank of Test",
                    RoutingNumber = "987654321"
                }
            },
            new()
            {
                Type = "swift",
                Swift = new StripeExeSwiftAddressDto()
                {
                    SwiftCode = "TESTUS33"
                }
            }
        };
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions",
            DisplayBankTransferInstructions = new StripeExeDisplayBankTransferInstructionsDto()
            {
                AmountRemaining = mockedAmountRemaining,
                Currency = mockedCurrency,
                Reference = mockedReference,
                FinancialAddresses = mockedFinancialAddresses
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = mockPaymentIntentDto.Amount,
                AmountRemaining = mockedAmountRemaining,
                BankTransferCurrency = mockedCurrency,
                PaymentProviderPaymentCurrency = mockPaymentIntentDto.Currency,
                Reference = mockedReference,
                FinancialAddresses = new FinancialAddresses()
                {
                    Aba = new Aba()
                    {
                        AccountNumber = "123456789",
                        BankName = "Bank of Test",
                        RoutingNumber = "987654321"
                    },
                    Swift = new Swift()
                    {
                        SwiftCode = "TESTUS33"
                    }
                }
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasNullFinancialAddresses_WhenMapBankTransferStrategy_ThenReturnsPendingStatusDetailsWithNullFinancialAddresses(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedAmountRemaining = 5000L;
        var mockedCurrency = "aud";
        var mockedReference = "REF123456";
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions",
            DisplayBankTransferInstructions = new StripeExeDisplayBankTransferInstructionsDto()
            {
                AmountRemaining = mockedAmountRemaining,
                Currency = mockedCurrency,
                Reference = mockedReference,
                FinancialAddresses = null
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = mockPaymentIntentDto.Amount,
                AmountRemaining = mockedAmountRemaining,
                BankTransferCurrency = mockedCurrency,
                PaymentProviderPaymentCurrency = mockPaymentIntentDto.Currency,
                Reference = mockedReference,
                FinancialAddresses = null
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasOnlySwiftFinancialAddress_WhenMapBankTransferStrategy_ThenReturnsPendingStatusDetailsWithOnlySwiftFinancialAddress(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedAmountRemaining = 5000L;
        var mockedCurrency = "aud";
        var mockedReference = "REF123456";
        var mockedFinancialAddresses = new List<StripeExeFinancialAddressDto>()
        {
            new()
            {
                Type = "swift",
                Swift = new StripeExeSwiftAddressDto()
                {
                    SwiftCode = "TESTUS33"
                }
            }
        };
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions",
            DisplayBankTransferInstructions = new StripeExeDisplayBankTransferInstructionsDto()
            {
                AmountRemaining = mockedAmountRemaining,
                Currency = mockedCurrency,
                Reference = mockedReference,
                FinancialAddresses = mockedFinancialAddresses
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = mockPaymentIntentDto.Amount,
                AmountRemaining = mockedAmountRemaining,
                BankTransferCurrency = mockedCurrency,
                PaymentProviderPaymentCurrency = mockPaymentIntentDto.Currency,
                Reference = mockedReference,
                FinancialAddresses = new FinancialAddresses()
                {
                    Aba = null,
                    Swift = new Swift()
                    {
                        SwiftCode = "TESTUS33"
                    }
                }
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasOnlyAbaFinancialAddress_WhenMapBankTransferStrategy_ThenReturnsPendingStatusDetailsWithOnlyAbaFinancialAddress(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedAmountRemaining = 5000L;
        var mockedCurrency = "aud";
        var mockedReference = "REF123456";
        var mockedFinancialAddresses = new List<StripeExeFinancialAddressDto>()
        {
            new()
            {
                Type = "aba",
                Aba = new StripeExeAbaAddressDto()
                {
                    AccountNumber = "123456789",
                    BankName = "Bank of Test",
                    RoutingNumber = "987654321"
                }
            }
        };
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions",
            DisplayBankTransferInstructions = new StripeExeDisplayBankTransferInstructionsDto()
            {
                AmountRemaining = mockedAmountRemaining,
                Currency = mockedCurrency,
                Reference = mockedReference,
                FinancialAddresses = mockedFinancialAddresses
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = mockPaymentIntentDto.Amount,
                AmountRemaining = mockedAmountRemaining,
                BankTransferCurrency = mockedCurrency,
                PaymentProviderPaymentCurrency = mockPaymentIntentDto.Currency,
                Reference = mockedReference,
                FinancialAddresses = new FinancialAddresses()
                {
                    Aba = new Aba()
                    {
                        AccountNumber = "123456789",
                        BankName = "Bank of Test",
                        RoutingNumber = "987654321"
                    },
                    Swift = null
                }
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }

    [Theory, ContractsAutoData]
    public void GivenDtoHasEmptyListOfFinancialAddresses_WhenMapBankTransferStrategy_ThenReturnsNullAbaAndSwift(
        StripeExePaymentIntentDto mockPaymentIntentDto)
    {
        // Arrange
        var mockedAmountRemaining = 5000L;
        var mockedCurrency = "aud";
        var mockedReference = "REF123456";
        var mockNextActionDto = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions",
            DisplayBankTransferInstructions = new StripeExeDisplayBankTransferInstructionsDto()
            {
                AmountRemaining = mockedAmountRemaining,
                Currency = mockedCurrency,
                Reference = mockedReference,
                FinancialAddresses = new List<StripeExeFinancialAddressDto>()
            }
        };
        mockPaymentIntentDto.NextAction = mockNextActionDto;

        // Act
        var result = _strategyUnderTest.Map(mockPaymentIntentDto, mockPaymentIntentDto.NextAction);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pendingStatusDetail = result.Value;
        var expectedPendingDetails = new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = mockPaymentIntentDto.Amount,
                AmountRemaining = mockedAmountRemaining,
                BankTransferCurrency = mockedCurrency,
                PaymentProviderPaymentCurrency = mockPaymentIntentDto.Currency,
                Reference = mockedReference,
                FinancialAddresses = new FinancialAddresses()
                {
                    Aba = null,
                    Swift = null
                }
            }
        };
        pendingStatusDetail.Should().BeEquivalentTo(expectedPendingDetails);
    }
}
