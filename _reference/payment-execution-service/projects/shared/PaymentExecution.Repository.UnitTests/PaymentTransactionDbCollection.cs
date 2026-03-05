using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionDbCollection
{
    public Mock<IDbConnection> DbConnectionMock { get; }
    public Mock<IDbTransaction> TransactionMock { get; }
    public Mock<IPaymentExecutionDbConnection> PaymentExecutionDbConnectionMock { get; }
    public Mock<IDapperWrapper> DapperWrapperMock { get; }
    public FakeTimeProvider FakeTimeProvider { get; }
    public PaymentTransactionRepository Subject { get; }

    public PaymentTransactionDbCollection()
    {
        DbConnectionMock = new Mock<IDbConnection>();
        PaymentExecutionDbConnectionMock = new Mock<IPaymentExecutionDbConnection>();
        DapperWrapperMock = new Mock<IDapperWrapper>();
        TransactionMock = new Mock<IDbTransaction>();
        FakeTimeProvider = new FakeTimeProvider();
        PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(DbConnectionMock.Object);
        PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        Subject = new PaymentTransactionRepository(new ConnectionFactory(
                new List<IPaymentExecutionDbConnection> { PaymentExecutionDbConnectionMock.Object }),
            new Mock<ILogger<PaymentTransactionRepository>>().Object, DapperWrapperMock.Object, FakeTimeProvider);
    }
}
