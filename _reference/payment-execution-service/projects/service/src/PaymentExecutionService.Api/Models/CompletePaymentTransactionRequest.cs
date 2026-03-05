using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PaymentExecution.Domain.Models;

namespace PaymentExecutionService.Models;

public record CompletePaymentTransactionRequest
{
    [RequiredOnSucceeded]
    [Range(0, int.MaxValue, ErrorMessage = "Fee must be a greater than 0.")]
    public decimal? Fee { get; set; }

    [RequiredOnSucceeded]
    public string? FeeCurrency { get; set; }

    /// <summary>
    ///PaymentIntentIntentId for Stripe
    /// </summary>
    [RequiredOnSucceeded]
    [RequiredOnCancelled]
    public string? PaymentProviderPaymentTransactionId { get; set; }

    /// <summary>
    ///ChargeId for stripe
    /// </summary>
    [RequiredOnSucceeded]
    public string? PaymentProviderPaymentReferenceId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ProviderType? ProviderType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TerminalStatus? Status { get; set; }

    [RequiredOnFailed]
    public string? FailureDetails { get; set; }

    public required DateTime EventCreatedDateTime { get; set; }

    [RequiredOnSucceeded]
    [RequiredOnCancelled]
    public DateTime? PaymentProviderLastUpdatedAt { get; set; }

    [RequiredOnCancelled]
    public string? CancellationReason { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class RequiredOnSucceededAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var request = (CompletePaymentTransactionRequest)validationContext.ObjectInstance;

        if (request.Status == TerminalStatus.Succeeded && (value == null || (value is string && string.IsNullOrWhiteSpace((string)value))))
        {
            return new ValidationResult($"The {validationContext.MemberName} field is required when the Status is Succeeded.");
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class RequiredOnFailedAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var request = (CompletePaymentTransactionRequest)validationContext.ObjectInstance;

        if (request.Status == TerminalStatus.Failed && (value == null || (value is string && string.IsNullOrWhiteSpace((string)value))))
        {
            return new ValidationResult($"The {validationContext.MemberName} field is required when the Status is Failed.");
        }

        return ValidationResult.Success;
    }
}


[AttributeUsage(AttributeTargets.Property)]
public class RequiredOnCancelledAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var request = (CompletePaymentTransactionRequest)validationContext.ObjectInstance;

        if (request.Status == TerminalStatus.Cancelled && (value == null || (value is string && string.IsNullOrWhiteSpace((string)value))))
        {
            return new ValidationResult($"The {validationContext.MemberName} field is required when the Status is Cancelled.");
        }

        return ValidationResult.Success;
    }
}
