using FluentResults;
using PaymentExecution.Domain.Models;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.Service.Strategies;

public interface IStripePendingActionStrategy
{
    string ActionType { get; }
    Result<PendingStatusDetails> Map(StripeExePaymentIntentDto paymentIntentDto, StripeExeNextActionDto nextActionDto);
}

public class BankTransferStrategy : IStripePendingActionStrategy
{
    public string ActionType => "display_bank_transfer_instructions";

    public Result<PendingStatusDetails> Map(StripeExePaymentIntentDto paymentIntentDto, StripeExeNextActionDto nextActionDto)
    {
        var financialAddresses = nextActionDto.DisplayBankTransferInstructions?.FinancialAddresses;

        var financialAddressesMapped = financialAddresses == null ? null :
            new FinancialAddresses()
            {
                Aba = financialAddresses
                    .Where(fa => fa.Type == "aba" && fa.Aba != null)
                    .Select(fa => new Aba()
                    {
                        AccountNumber = fa.Aba!.AccountNumber,
                        BankName = fa.Aba!.BankName,
                        RoutingNumber = fa.Aba!.RoutingNumber
                    })
                    .FirstOrDefault(),
                Swift = financialAddresses
                    .Where(fa => fa.Type == "swift" && fa.Swift != null)
                    .Select(fa => new Swift()
                    {
                        SwiftCode = fa.Swift!.SwiftCode
                    })
                    .FirstOrDefault()
            };

        return new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.BankTransferInstructions,
            HasActionValue = true,
            BankTransferInstructions = new BankTransferInstructions()
            {
                Amount = paymentIntentDto.Amount,
                AmountRemaining = nextActionDto.DisplayBankTransferInstructions?.AmountRemaining,
                BankTransferCurrency = nextActionDto.DisplayBankTransferInstructions?.Currency,
                PaymentProviderPaymentCurrency = paymentIntentDto.Currency,
                Reference = nextActionDto.DisplayBankTransferInstructions?.Reference,
                FinancialAddresses = financialAddressesMapped
            }
        };
    }
}

public class RedirectToUrlStrategy : IStripePendingActionStrategy
{
    public string ActionType => "redirect_to_url";

    public Result<PendingStatusDetails> Map(StripeExePaymentIntentDto paymentIntentDto, StripeExeNextActionDto nextActionDto)
    {
        return new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.RedirectToUrl,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = nextActionDto.RedirectToUrl?.Url
            }
        };
    }
}

public class MicrodepositVerificationStrategy : IStripePendingActionStrategy
{
    public string ActionType => "verify_with_microdeposits";

    public Result<PendingStatusDetails> Map(StripeExePaymentIntentDto paymentIntentDto, StripeExeNextActionDto nextActionDto)
    {
        return new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.MicrodepositVerification,
            HasActionValue = nextActionDto.VerifyWithMicrodeposits != null,
            MicroDepositVerification = nextActionDto.VerifyWithMicrodeposits == null ? null : new MicroDepositVerification()
            {
                HostedVerificationUrl = nextActionDto.VerifyWithMicrodeposits.HostedVerificationUrl,
                BankAccountLast4 = paymentIntentDto.PaymentMethod?.UsBankAccount?.Last4
            }
        };
    }
}

public class PayToAuthorizationStrategy : IStripePendingActionStrategy
{
    public string ActionType => "payto_await_authorization";

    public Result<PendingStatusDetails> Map(StripeExePaymentIntentDto paymentIntentDto, StripeExeNextActionDto nextActionDto)
    {
        return new PendingStatusDetails()
        {
            RequiresActionType = RequiresActionType.PayToAuthorization,
            HasActionValue = false
        };
    }
}
