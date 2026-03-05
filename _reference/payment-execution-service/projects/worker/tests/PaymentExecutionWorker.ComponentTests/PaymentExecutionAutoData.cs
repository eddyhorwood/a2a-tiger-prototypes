using AutoFixture;
using AutoFixture.Xunit2;
using PaymentExecution.Domain.Models;

namespace PaymentExecutionWorker.ComponentTests;

public static class PaymentExecutionFixtureProvider
{
    public static readonly IFixture Fixture = new Fixture()
        .Customize(new DtoAutoFixtureCustomisation())
        .Customize(new ExecutionQueueMessageAutoFixtureCustomisation());
}

public class DtoAutoFixtureCustomisation : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<PaymentExecution.Repository.Models.PaymentTransactionDto>(c =>
            c.With(p => p.ProviderType, nameof(ProviderType.Stripe))
                .With(p => p.Fee, (decimal?)null)
                .With(p => p.FeeCurrency, "aud")
                .With(p => p.EventCreatedDateTimeUtc, DateTime.UtcNow.AddMinutes(-1)) // DTO by default is less recent than message being processed
                .With(p => p.PaymentProviderPaymentTransactionId, Utilities.GenerateMockStripeIdFromPrefix("pi", fixture))
                .With(p => p.PaymentProviderPaymentReferenceId, Utilities.GenerateMockStripeIdFromPrefix("ch", fixture)));
    }
}

public class ExecutionQueueMessageAutoFixtureCustomisation : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<ExecutionQueueMessage>(c =>
            c.With(p => p.ProviderType, nameof(ProviderType.Stripe))
                .With(p => p.Fee, Random.Shared.Next(1, 10) / 100.0m)
                .With(p => p.FeeCurrency, "aud")
                .With(p => p.EventCreatedDateTime, DateTime.UtcNow.AddMinutes(1)) // Message being processed by default is more recent than DTO
                .With(p => p.PaymentProviderPaymentReferenceId, Utilities.GenerateMockStripeIdFromPrefix("ch", fixture)));
    }
}

public static class Utilities
{
    public static string GenerateMockStripeIdFromPrefix(string prefix, IFixture fixture)
    {
        const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomPart = string.Join("", fixture.CreateMany<char>(10).Select(c => Chars[Math.Abs(c) % Chars.Length]));
        return $"{prefix}_{randomPart}";
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class PaymentExecutionDataAttribute()
    : AutoDataAttribute(() => PaymentExecutionFixtureProvider.Fixture);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlinePaymentExecutionAutoDataAttribute(params object?[] objects)
    : InlineAutoDataAttribute(new PaymentExecutionDataAttribute(), objects);
