using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace PaymentExecution.Repository;

public interface IPaymentExecutionDbConnection
{
    string DatabaseName { get; }
    IDbConnection GetConnection();
}

public class PaymentExecutionDbConnection(IOptions<PaymentExecutionDbConnectionOptions> options)
    : IPaymentExecutionDbConnection
{
    private readonly PaymentExecutionDbConnectionOptions _options = options.Value;

    public string DatabaseName => "PaymentExecutionDB";

    public IDbConnection GetConnection()
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_options.ConnectionString)
        {
            MinPoolSize = _options.MinPoolSize
        };

        return new NpgsqlConnection(connectionStringBuilder.ConnectionString);
    }
}
