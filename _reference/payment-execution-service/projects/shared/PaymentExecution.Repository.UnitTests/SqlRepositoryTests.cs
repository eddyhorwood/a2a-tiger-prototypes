using Moq;

namespace PaymentExecution.Repository.UnitTests;

public class SqlRepositoryTests
{
    private readonly Mock<IConnectionFactory> _connectionFactory = new();
    private const string ValidDatabaseName = "test_db";
    private const string ValidTableName = "test_table";

    [Fact]
    public void GivenValidTableName_WhenConstructorIsInvoked_ShouldCreateSqlRepository()
    {
        // Arrange & Act
        var repository = new TestSqlRepository(_connectionFactory.Object, ValidDatabaseName, ValidTableName);

        // Assert
        Assert.NotNull(repository);
        Assert.Equal(ValidTableName, repository.GetTableName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenInValidTableName_WhenConstructorIsInvoked_ShouldThrowException(string? invalidTableName)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new TestSqlRepository(_connectionFactory.Object, ValidDatabaseName, invalidTableName));

        Assert.Equal("tableName", exception.ParamName);
        Assert.Contains("tableName is null or empty", exception.Message);
    }

    private class TestSqlRepository(IConnectionFactory connectionFactory, string databaseName, string tableName)
        : SqlRepository(connectionFactory, databaseName, tableName)
    {
        public string GetTableName() => TableName;
    }
}
