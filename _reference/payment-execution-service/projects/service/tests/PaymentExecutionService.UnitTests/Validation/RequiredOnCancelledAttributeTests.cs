using System;
using System.ComponentModel.DataAnnotations;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Models;
using Xunit;
namespace PaymentExecutionService.UnitTests.Validation;

public class RequiredOnCancelledAttributeTests
{
    private readonly RequiredOnCancelledAttribute _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GivenCancelledStatusInRequest_WhenAttributedValueIsEmptyOrNull_ThenRequiredOnCancelledAttributeShouldReturnValidationResult(string? invalidVal)
    {
        var validationContext = new ValidationContext(new CompletePaymentTransactionRequest()
        {
            Status = TerminalStatus.Cancelled,
            ProviderType = ProviderType.Stripe,
            EventCreatedDateTime = DateTime.UtcNow
        }, null, null);

        var result = _sut.GetValidationResult(invalidVal, validationContext);

        Assert.IsType<ValidationResult>(result);
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenCancelledStatusInRequest_WhenAttributedValueIsNotNullOrWhiteSpace_ThenRequiredOnCancelledAttributeShouldReturnValidationSuccess()
    {
        var validString = "some-value-string";
        var validationContext = new ValidationContext(new CompletePaymentTransactionRequest()
        {
            Status = TerminalStatus.Cancelled,
            ProviderType = ProviderType.Stripe,
            EventCreatedDateTime = DateTime.UtcNow
        }, null, null);

        var result = _sut.GetValidationResult(validString, validationContext);

        Assert.Equal(ValidationResult.Success, result);
    }
}
