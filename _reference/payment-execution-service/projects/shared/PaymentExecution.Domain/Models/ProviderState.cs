using System.Diagnostics.CodeAnalysis;

namespace PaymentExecution.Domain.Models;

//INPAY-23791 To remove sonar exclusions with implementation of business logic
[ExcludeFromCodeCoverage]
public class ProviderState
{
    public PaymentProviderStatus PaymentProviderStatus { get; set; }
    public required string PaymentProviderPaymentTransactionId { get; set; }
    public ProviderType ProviderType { get; set; }
    public string? LastPaymentErrorCode { get; set; }
    public PendingStatusDetails? PendingStatusDetails { get; set; }
}

[ExcludeFromCodeCoverage]
public class PendingStatusDetails
{
    public RequiresActionType RequiresActionType { get; set; }
    public PaymentMethodType PaymentMethodType { get; set; }
    public bool HasActionValue { get; set; }
    public BankTransferInstructions? BankTransferInstructions { get; set; }
    public RedirectToUrl? RedirectToUrl { get; set; }
    public MicroDepositVerification? MicroDepositVerification { get; set; }
}

[ExcludeFromCodeCoverage]
public class BankTransferInstructions
{
    public FinancialAddresses? FinancialAddresses { get; set; }
    public long Amount { get; set; }
    public required string PaymentProviderPaymentCurrency { get; set; }
    public string? BankTransferCurrency { get; set; }
    public long? AmountRemaining { get; set; }
    public string? Reference { get; set; }
}

[ExcludeFromCodeCoverage]
public class FinancialAddresses
{
    public Aba? Aba { get; set; }
    public Swift? Swift { get; set; }
}

[ExcludeFromCodeCoverage]
public class Aba
{
    public required string AccountNumber { get; set; }
    public required string BankName { get; set; }
    public required string RoutingNumber { get; set; }
}

[ExcludeFromCodeCoverage]
public class Swift
{
    public required string SwiftCode { get; set; }
}

[ExcludeFromCodeCoverage]
public class MicroDepositVerification
{
    public required string HostedVerificationUrl { get; set; }
    public string? BankAccountLast4 { get; set; }
}

[ExcludeFromCodeCoverage]
public class RedirectToUrl
{
    public string? RedirectUrl { get; set; }
}
