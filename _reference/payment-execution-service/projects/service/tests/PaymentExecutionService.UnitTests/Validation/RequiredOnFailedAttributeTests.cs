using System;
using System.ComponentModel.DataAnnotations;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Models;
using Xunit;

namespace PaymentExecutionService.UnitTests.Validation;

public class RequiredOnFailedAttributeTests
{
    private readonly RequiredOnFailedAttribute _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GivenFailedStatusInRequest_WhenAttributedValueIsEmptyOrNull_ThenShouldReturnValidationResult(string? val)
    {
        var validationContext = new ValidationContext(new CompletePaymentTransactionRequest()
        {
            Status = TerminalStatus.Failed,
            ProviderType = ProviderType.Stripe,
            EventCreatedDateTime = DateTime.UtcNow
        }, null, null);

        var result = _sut.GetValidationResult(val, validationContext);

        Assert.IsType<ValidationResult>(result);
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenFailedStatusInRequest_WhenAttributedValueIsNotNullOrWhiteSpace_ShouldReturnValidationSuccess()
    {
        var validString = "some-value-string";
        var validationContext = new ValidationContext(new CompletePaymentTransactionRequest()
        {
            Status = TerminalStatus.Failed,
            ProviderType = ProviderType.Stripe,
            EventCreatedDateTime = DateTime.UtcNow
        }, null, null);

        var result = _sut.GetValidationResult(validString, validationContext);

        Assert.Equal(ValidationResult.Success, result);
    }
}
