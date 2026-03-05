using AutoFixture;
using AutoFixture.Xunit2;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.UnitTests;

[AttributeUsage(AttributeTargets.Method)]
public class ContractsAutoDataAttribute()
    : AutoDataAttribute(() => new Fixture().Customize(new StripeExeNextActionDtoCustomisation()));

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InlineContractsAutoDataAttribute(params object?[] objects)
    : InlineAutoDataAttribute(new ContractsAutoDataAttribute(), objects);

public class StripeExeNextActionDtoCustomisation : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<StripeExeNextActionDto>(c => c.Without(x => x.Misc));
    }
}

