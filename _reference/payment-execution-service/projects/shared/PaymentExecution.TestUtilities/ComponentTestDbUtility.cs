using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using PaymentExecution.Repository;

namespace PaymentExecution.TestUtilities;

public class PaymentExecutionComponentTestDbConnection(IOptions<PaymentExecutionDbConnectionOptions> options)
    : IPaymentExecutionDbConnection
{
    private readonly PaymentExecutionDbConnectionOptions _options = options.Value;

    public string DatabaseName { get; set; } = "PaymentExecutionDB";

    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_options.ConnectionString);
    }
}

public class ConnectionFactory(IEnumerable<IPaymentExecutionDbConnection> dbConnections) : IConnectionFactory
{
    public IPaymentExecutionDbConnection GetConnection(string databaseName)
    {
        return dbConnections.FirstOrDefault(connection => connection.DatabaseName == databaseName) ??
               throw new ArgumentException($"connection for database: {databaseName} not found");
    }
}

public interface IPaymentTransactionComponentTestRepository
{
    Task WipeDb();

    Task InsertMockSubmittedPaymentTransaction(
        Repository.Models.PaymentTransactionDto paymentTransactionDto);

    Task InsertMockSubmittedAndMockProcessedPaymentTransaction(
        Repository.Models.PaymentTransactionDto paymentTransactionDto);

    Task InsertMockPaymentTransaction(
        Repository.Models.PaymentTransactionDto paymentTransactionDto,
        Guid? organisationId = null);
}


public class PaymentTransactionComponentTestRepository(
    IConnectionFactory connectionFactory,
    IDapperWrapper dapperWrapper
) : SqlRepository(
        connectionFactory,
        DatabaseName,
        PaymentTransactionTable
    ),
    IPaymentTransactionComponentTestRepository
{
    private const string PaymentTransactionTable = "PaymentTransaction";
    private const string DatabaseName = "PaymentExecutionDB";

    public async Task InsertMockSubmittedPaymentTransaction(
        Repository.Models.PaymentTransactionDto paymentTransactionDto)
    {
        const string Sql =
            @"INSERT INTO payment_execution.PaymentTransaction 
                (PaymentRequestId,
                ProviderType,
                Status,
                PaymentProviderPaymentTransactionId,
                ProviderServiceId,
                OrganisationId,
                CreatedUtc,
                 UpdatedUtc,
                 CancellationReason)    
              VALUES 
                (@PaymentRequestId, 
                  @ProviderType, 
                  @Status, 
                  @PaymentProviderPaymentTransactionId, 
                  @ProviderServiceId, 
                 @OrganisationId,
                 @CreatedUtc,
                 @UpdatedUtc,
                 @CancellationReason);";

        using var conn = GetConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        await dapperWrapper.ExecuteAsync(conn, Sql, new
        {
            paymentTransactionDto.PaymentRequestId,
            paymentTransactionDto.ProviderType,
            paymentTransactionDto.Status,
            paymentTransactionDto.PaymentProviderPaymentTransactionId,
            paymentTransactionDto.ProviderServiceId,
            OrganisationId = Guid.NewGuid(),
            paymentTransactionDto.CreatedUtc,
            paymentTransactionDto.UpdatedUtc,
            paymentTransactionDto.CancellationReason
        }, transaction);
        transaction.Commit();
        conn.Close();
    }

    public async Task InsertMockSubmittedAndMockProcessedPaymentTransaction(Repository.Models.PaymentTransactionDto paymentTransactionDto)
    {
        const string Sql =
            @"INSERT INTO payment_execution.PaymentTransaction 
                (PaymentRequestId,
                ProviderType,
                Status,
                PaymentProviderPaymentReferenceId,
                PaymentProviderPaymentTransactionId,
                ProviderServiceId,
                Fee,
                FeeCurrency,
                EventCreatedDateTimeUtc,
                 OrganisationId,
                 CreatedUtc,
                 UpdatedUtc)    
              VALUES 
                (@PaymentRequestId, 
                  @ProviderType, 
                  @Status, 
                  @PaymentProviderPaymentReferenceId, 
                  @PaymentProviderPaymentTransactionId, 
                  @ProviderServiceId, 
                  @Fee, 
                  @FeeCurrency, 
                  @EventCreatedDateTimeUtc,
                 @OrganisationId,
                 @CreatedUtc,
                 @UpdatedUtc);";

        using var conn = GetConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        await dapperWrapper.ExecuteAsync(conn, Sql, new
        {
            paymentTransactionDto.PaymentRequestId,
            paymentTransactionDto.ProviderType,
            paymentTransactionDto.Status,
            paymentTransactionDto.PaymentProviderPaymentReferenceId,
            paymentTransactionDto.PaymentProviderPaymentTransactionId,
            paymentTransactionDto.ProviderServiceId,
            paymentTransactionDto.Fee,
            paymentTransactionDto.FeeCurrency,
            paymentTransactionDto.EventCreatedDateTimeUtc,
            OrganisationId = Guid.NewGuid(),
            paymentTransactionDto.CreatedUtc,
            paymentTransactionDto.UpdatedUtc
        }, transaction);
        transaction.Commit();
        conn.Close();
    }

    public async Task WipeDb()
    {
        using var conn = GetConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        await dapperWrapper.ExecuteAsync(conn, "DELETE FROM payment_execution.PaymentTransaction", transaction);
        transaction.Commit();
        conn.Close();
    }

    public async Task InsertMockPaymentTransaction(
        Repository.Models.PaymentTransactionDto paymentTransactionDto,
        Guid? organisationId = null)
    {
        const string Sql =
            @"INSERT INTO payment_execution.PaymentTransaction
                (PaymentRequestId, ProviderType, Status,
                 PaymentProviderPaymentTransactionId, ProviderServiceId,
                 OrganisationId, CreatedUtc, UpdatedUtc, CancellationReason)
              VALUES (@PaymentRequestId, @ProviderType, @Status,
                      @PaymentProviderPaymentTransactionId, @ProviderServiceId,
                      @OrganisationId, @CreatedUtc, @UpdatedUtc, @CancellationReason);";

        var orgId = organisationId ?? Guid.NewGuid();

        using var conn = GetConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        await dapperWrapper.ExecuteAsync(conn, Sql, new
        {
            paymentTransactionDto.PaymentRequestId,
            paymentTransactionDto.ProviderType,
            paymentTransactionDto.Status,
            paymentTransactionDto.PaymentProviderPaymentTransactionId,
            paymentTransactionDto.ProviderServiceId,
            OrganisationId = orgId,
            paymentTransactionDto.CreatedUtc,
            paymentTransactionDto.UpdatedUtc,
            paymentTransactionDto.CancellationReason
        }, transaction);
        transaction.Commit();
        conn.Close();
    }
}

