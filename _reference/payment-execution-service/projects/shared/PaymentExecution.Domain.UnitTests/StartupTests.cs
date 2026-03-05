using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.Domain.Service.Strategies;

namespace PaymentExecution.Domain.UnitTests;

public class StartupTests
{
    [Fact]
    public void GivenApiServiceCollection_WhenAddProviderHandlerExtensionMethod_ThenRegistersExpectedPendingActionStrategies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddProviderIntegrationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var actionStrategies = serviceProvider.GetService<IEnumerable<IStripePendingActionStrategy>>();
        actionStrategies.Should().NotBeNull();
        var strategies = actionStrategies.ToList();

        strategies.Should().HaveCount(4);
        strategies.Select(x => x.GetType()).Should().Contain([
            typeof(BankTransferStrategy),
            typeof(RedirectToUrlStrategy),
            typeof(MicrodepositVerificationStrategy),
            typeof(PayToAuthorizationStrategy)
        ]);
    }
}
