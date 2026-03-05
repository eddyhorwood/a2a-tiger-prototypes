using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentExecutionDbConnectionTests
{
    private const string BaseConnectionString =
        "Server=localhost;Database=payment_execution_db;User Id=payment_execution_user;Password=temp_p@ssw0rd;";

    [Fact]
    public void GivenValidOptions_WhenInstantiated_ThenDatabaseNameShouldBeSet()
    {
        // Arrange
        var optionsMock = Options.Create(new PaymentExecutionDbConnectionOptions
        {
            ConnectionString = BaseConnectionString
        });

        // Act
        var sut = new PaymentExecutionDbConnection(optionsMock);

        // Assert
        sut.DatabaseName.Should().BeSameAs("PaymentExecutionDB");
    }

    [Fact]
    public void GivenMinPoolSizeNotInOptions_WhenGetConnection_ThenReturnsExpectedConnectionStringWithDefaultMinPoolSize()
    {
        // Arrange
        var optionsMock = Options.Create(new PaymentExecutionDbConnectionOptions
        {
            ConnectionString = BaseConnectionString
        });
        var expectedConnectionString =
            "Host=localhost;Database=payment_execution_db;Username=payment_execution_user;Password=temp_p@ssw0rd;Minimum Pool Size=0";
        var sut = new PaymentExecutionDbConnection(optionsMock);

        // Act
        using var connection = sut.GetConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
        connection.ConnectionString.Should().Be(expectedConnectionString);
    }

    [Theory]
    [InlineAutoData(4)]
    [InlineAutoData(2)]
    [InlineAutoData(3)]
    public void GivenValidOptionsWithMinPoolSize_WhenGetConnectionCalled_ThenReturnsNpgsqlConnectionWithSetPoolSize(int minPoolSize)
    {
        // Arrange
        var optionsMock = Options.Create(new PaymentExecutionDbConnectionOptions
        {
            ConnectionString = BaseConnectionString,
            MinPoolSize = minPoolSize
        });
        var expectedConnectionString =
            $"Host=localhost;Database=payment_execution_db;Username=payment_execution_user;Password=temp_p@ssw0rd;Minimum Pool Size={minPoolSize}";
        var sut = new PaymentExecutionDbConnection(optionsMock);

        // Act
        using var connection = sut.GetConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
        connection.ConnectionString.Should().Be(expectedConnectionString);
    }

    [Fact]
    public void GivenPoolSizeConfigurationSetInBaseConnectionString_WhenGetConnection_ThenOverridesMinPoolWithValueInOptions()
    {
        //Arrange
        var baseConnectionString = "Server=test;Database=test_db;User Id=test;Password=test;Minimum Pool Size=10;";
        var minPoolSize = 2;
        var paymentRequestDbConnectionOptions =
            Options.Create(
                new PaymentExecutionDbConnectionOptions
                {
                    ConnectionString = baseConnectionString,
                    MinPoolSize = minPoolSize
                });
        var expectedConnectionString = $"Host=test;Database=test_db;Username=test;Password=test;Minimum Pool Size={minPoolSize}";
        var sut = new PaymentExecutionDbConnection(paymentRequestDbConnectionOptions);

        //Act
        using var connection = sut.GetConnection();

        //Assert
        connection.ConnectionString.Should().Be(expectedConnectionString);
    }
}
