using AutoFixture;
using AutoFixture.Xunit2;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.TestUtilities;

public class CancelLambdaAutoDataAttribute : AutoDataAttribute
{
    public CancelLambdaAutoDataAttribute() : base(() => new Fixture().Customize(new CancelLambdaCustomization()))
    {
    }
}

public class CancelLambdaCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<InsertPaymentTransactionDto>(composer => composer
            .With(x => x.Status, "Submitted")
            .With(x => x.ProviderType, "Stripe"));

        fixture.Customize<PaymentTransactionDto>(composer => composer
            .With(x => x.Status, "Submitted")
            .With(x => x.ProviderType, "Stripe")
            .With(x => x.CreatedUtc, DateTime.UtcNow)
            .With(x => x.UpdatedUtc, DateTime.UtcNow));
    }
}

