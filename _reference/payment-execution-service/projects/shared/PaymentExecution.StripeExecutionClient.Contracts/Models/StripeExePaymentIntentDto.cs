using System.Text.Json;

namespace PaymentExecution.StripeExecutionClient.Contracts.Models;

public class StripeExePaymentIntentDto
{
    public required string Id { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public StripeExeNextActionDto? NextAction { get; set; }
    public StripeExePaymentMethodDto? PaymentMethod { get; set; }
    public StripeExeLastPaymentErrorDto? LastPaymentError { get; set; }
}

public class StripeExeNextActionDto
{
    public required string Type { get; set; }
    public StripeExeRedirectToUrlDto? RedirectToUrl { get; set; }
    public StripeExeDisplayBankTransferInstructionsDto? DisplayBankTransferInstructions { get; set; }

    public StripeExeVerifyWithMicrodepositsDto? VerifyWithMicrodeposits { get; set; }

    // Catch-all for unknown future next_action types
    public JsonElement? Misc { get; set; }
}

public class StripeExeRedirectToUrlDto
{
    public string? Url { get; set; }
}

public class StripeExeDisplayBankTransferInstructionsDto
{
    public long? AmountRemaining { get; set; }
    public string? Currency { get; set; }
    public string? Reference { get; set; }
    public List<StripeExeFinancialAddressDto>? FinancialAddresses { get; set; }
}

public class StripeExeFinancialAddressDto
{
    public required string Type { get; set; }
    public StripeExeAbaAddressDto? Aba { get; set; }
    public StripeExeSwiftAddressDto? Swift { get; set; }
}

public class StripeExeAbaAddressDto
{
    public required string AccountNumber { get; set; }
    public required string BankName { get; set; }
    public required string RoutingNumber { get; set; }
}

public class StripeExeSwiftAddressDto
{
    public required string SwiftCode { get; set; }
}

public class StripeExeVerifyWithMicrodepositsDto
{
    public required string HostedVerificationUrl { get; set; }
}

public class StripeExePaymentMethodDto
{
    public required string Type { get; set; }
    public StripeExeUsBankAccountDto? UsBankAccount { get; set; }
}

public class StripeExeUsBankAccountDto
{
    public string? Last4 { get; set; }
}

public class StripeExeLastPaymentErrorDto
{
    public string? Code { get; set; }
    public string? DeclineCode { get; set; }
    public string? PaymentMethodType { get; set; }
}
