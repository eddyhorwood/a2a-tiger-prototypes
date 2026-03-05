using System.ComponentModel.DataAnnotations;

namespace PaymentExecutionService.Models;

public class SubmitStripeRequest : IValidatableObject
{
    public required Guid PaymentRequestId { get; set; }

    [MaxLength(999)]
    public List<string>? PaymentMethodsMadeAvailable { get; set; }
    public string? PaymentMethodId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentMethodsMadeAvailable == null && string.IsNullOrWhiteSpace(PaymentMethodId))
        {
            yield return new ValidationResult(
                "Either PaymentMethodsMadeAvailable or PaymentMethodId must have a value.",
                new[] { nameof(PaymentMethodsMadeAvailable), nameof(PaymentMethodId) });
        }
    }
}
