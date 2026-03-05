using System.Text.Json;
using FluentAssertions;
using PaymentExecutionLambda.CancelLambda.Extensions;
using PaymentExecutionLambda.CancelLambda.Models;

namespace PaymentExecutionLambda.CancelLambdaUnitTests.Extensions;

public class JsonSerializerExtensionsTests
{
    [Fact]
    public void GivenValidJson_WhenTryDeserialize_ThenReturnsTrueAndDeserializesObject()
    {
        // Arrange
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };
        var validJson = JsonSerializer.Serialize(cancelRequest);

        // Act
        var result = JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(validJson, out var deserializedObject);

        // Assert
        result.Should().BeTrue();
        deserializedObject.Should().NotBeNull();
        deserializedObject.PaymentRequestId.Should().Be(cancelRequest.PaymentRequestId);
        deserializedObject.ProviderType.Should().Be(cancelRequest.ProviderType);
        deserializedObject.CancellationReason.Should().Be(cancelRequest.CancellationReason);
    }

    [Fact]
    public void GivenInvalidJson_WhenTryDeserialize_ThenReturnsFalseAndSetsDefaultValue()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(invalidJson, out var deserializedObject);

        // Assert
        result.Should().BeFalse();
        deserializedObject.Should().BeNull();
    }

    [Fact]
    public void GivenPascalCaseJson_WhenTryDeserialize_ThenReturnsTrueAndDeserializesObject()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var pascalCaseJson = $$"""
        {
            "PaymentRequestId": "{{paymentRequestId}}",
            "ProviderType": "Stripe",
            "CancellationReason": "Customer request"
        }
        """;

        // Act
        var result = JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(pascalCaseJson, out var deserializedObject);

        // Assert
        result.Should().BeTrue();
        deserializedObject.Should().NotBeNull();
        deserializedObject.PaymentRequestId.Should().Be(paymentRequestId);
        deserializedObject.ProviderType.Should().Be("Stripe");
        deserializedObject.CancellationReason.Should().Be("Customer request");
    }

    [Fact]
    public void GivenCamelCaseJson_WhenTryDeserialize_ThenReturnsTrueAndDeserializesObject()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var camelCaseJson = $$"""
        {
            "paymentRequestId": "{{paymentRequestId}}",
            "providerType": "Stripe",
            "cancellationReason": "Customer request"
        }
        """;

        // Act
        var result = JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(camelCaseJson, out var deserializedObject);

        // Assert
        result.Should().BeTrue();
        deserializedObject.Should().NotBeNull();
        deserializedObject.PaymentRequestId.Should().Be(paymentRequestId);
        deserializedObject.ProviderType.Should().Be("Stripe");
        deserializedObject.CancellationReason.Should().Be("Customer request");
    }

    [Fact]
    public void GivenMixedCaseJson_WhenTryDeserialize_ThenReturnsTrueAndDeserializesObject()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mixedCaseJson = $$"""
        {
            "PAYMENTREQUESTID": "{{paymentRequestId}}",
            "providertype": "Stripe",
            "CancellationReason": "Customer request"
        }
        """;

        // Act
        var result = JsonSerializerExtensions.TryDeserialize<CancelPaymentRequest>(mixedCaseJson, out var deserializedObject);

        // Assert
        result.Should().BeTrue();
        deserializedObject.Should().NotBeNull();
        deserializedObject.PaymentRequestId.Should().Be(paymentRequestId);
        deserializedObject.ProviderType.Should().Be("Stripe");
        deserializedObject.CancellationReason.Should().Be("Customer request");
    }
}
