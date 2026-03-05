using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PaymentExecution.Repository.UnitTests;


public class PaymentExecutionDbConnectionOptionsTests
{
    [Fact]
    public void GivenValidConfiguration_WhenBinding_ThenOptionsShouldBeCorrectlySet()
    {
        // Arrange
        var expectedConnectionString = "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;";
        var inMemorySettings = new Dictionary<string, string>
        {
            { "DataAccess:PaymentExecutionDB:ConnectionString", expectedConnectionString }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var services = new ServiceCollection();
        services.Configure<PaymentExecutionDbConnectionOptions>(configuration.GetSection(PaymentExecutionDbConnectionOptions.Key));
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<PaymentExecutionDbConnectionOptions>>().Value;

        // Act & Assert
        Assert.NotNull(options);
        expectedConnectionString.Should().BeSameAs(options.ConnectionString);
    }
}
