namespace PaymentExecution.Repository;

public interface IConnectionFactory
{
    IPaymentExecutionDbConnection GetConnection(string databaseName);
}

public class ConnectionFactory(IEnumerable<IPaymentExecutionDbConnection> dbConnections) : IConnectionFactory
{
    public IPaymentExecutionDbConnection GetConnection(string databaseName)
    {
        return dbConnections.FirstOrDefault(connection => connection.DatabaseName == databaseName) ??
               throw new ArgumentException($"connection for database: {databaseName} not found");
    }
}
