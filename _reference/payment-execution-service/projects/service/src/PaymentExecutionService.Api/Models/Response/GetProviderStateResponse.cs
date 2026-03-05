using System.Text.Json.Serialization;

namespace PaymentExecutionService.Models.Response;

public class GetProviderStateResponse
{
    public PaymentProviderStatusResponse PaymentProviderStatus { get; init; }
    public required string PaymentProviderPaymentTransactionId { get; init; }
    public ProviderTypeResponse ProviderType { get; init; }
    public string? LastPaymentErrorCode { get; init; }
    public PendingStatusDetailsResponse? PendingStatusDetails { get; init; }
}

public class PendingStatusDetailsResponse
{
    public PaymentMethodTypeResponse PaymentMethodType { get; set; }
    public RequiresActionTypeResponse RequiresActionType { get; set; }
    public bool HasActionValue { get; set; }
    public BankTransferInstructionsResponse? BankTransferInstructions { get; set; }
    public RedirectToUrlResponse? RedirectToUrl { get; set; }
    public MicroDepositVerificationResponse? MicroDepositVerification { get; set; }
}

public class BankTransferInstructionsResponse
{
    public FinancialAddressesResponse? FinancialAddresses { get; set; }
    public long Amount { get; set; }
    public required string PaymentProviderPaymentCurrency { get; set; }
    public string? BankTransferCurrency { get; set; }
    public long? AmountRemaining { get; set; }
    public string? Reference { get; set; }
}

public class FinancialAddressesResponse
{
    public AbaResponse? Aba { get; set; }
    public SwiftResponse? Swift { get; set; }
}

public class SwiftResponse
{
    public required string SwiftCode { get; set; }
}

public class AbaResponse
{
    public required string AccountNumber { get; set; }
    public required string BankName { get; set; }
    public required string RoutingNumber { get; set; }
}

public class RedirectToUrlResponse
{
    public string? RedirectUrl { get; set; }
}

public class MicroDepositVerificationResponse
{
    public required string HostedVerificationUrl { get; set; }
    public string? BankAccountLast4 { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentProviderStatusResponse
{
    Submitted,
    Processing,
    RequiresAction,
    Terminal
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderTypeResponse
{
    Stripe,
    Paypal,
    GoCardless
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethodTypeResponse
{
    Card,
    Link,
    CustomerBalance,
    AfterPay,
    AuBecsDebit,
    BacsDebit,
    Klarna,
    UsBankAccount,
    Zip,
    PayTo,
    PayByBank,
    ApplePay,
    GooglePay
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequiresActionTypeResponse
{
    BankTransferInstructions,
    RedirectToUrl,
    MicrodepositVerification,
    PayToAuthorization,
    Unknown,
    None
}
