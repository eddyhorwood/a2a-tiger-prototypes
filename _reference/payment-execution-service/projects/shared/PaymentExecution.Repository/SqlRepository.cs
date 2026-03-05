using System.Data;

namespace PaymentExecution.Repository;

public abstract class SqlRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _databaseName;

    protected readonly string TableName;

    protected SqlRepository(IConnectionFactory connectionFactory, string databaseName, string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentNullException(nameof(tableName), $"{nameof(tableName)} is null or empty");
        }

        _connectionFactory = connectionFactory;
        _databaseName = databaseName;
        TableName = tableName;
    }

    protected IDbConnection GetConnection()
    {
        return _connectionFactory.GetConnection(_databaseName).GetConnection();
    }
}
