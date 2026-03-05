using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTestsHealthCheck
{

    private class PaymentTransactionDbCollection
    {
        public Mock<IDbConnection> DbConnectionMock { get; }
        public Mock<IPaymentExecutionDbConnection> PaymentExecutionDbConnectionMock { get; }
        public Mock<IDapperWrapper> DapperWrapperMock { get; }
        public PaymentTransactionRepository Subject { get; }

        public PaymentTransactionDbCollection()
        {
            DbConnectionMock = new Mock<IDbConnection>();
            PaymentExecutionDbConnectionMock = new Mock<IPaymentExecutionDbConnection>();
            DapperWrapperMock = new Mock<IDapperWrapper>();
            Subject = new PaymentTransactionRepository(new ConnectionFactory(
                new List<IPaymentExecutionDbConnection>()
                {
                    PaymentExecutionDbConnectionMock.Object
                }), new Mock<ILogger<PaymentTransactionRepository>>().Object, DapperWrapperMock.Object, new FakeTimeProvider());
        }
    }

    private readonly PaymentTransactionDbCollection _context = new();

    [Fact]
    public async Task GivenDBIsHealthy_WhenHealthCheck_ThenHealthCheckResultIsTrue()
    {
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DapperWrapperMock.Setup(m => m.QueryAsync<int>(_context.DbConnectionMock.Object, It.IsAny<string>(), null)).ReturnsAsync(It.IsAny<IEnumerable<int>>());

        var result = await _context.Subject.HealthCheck();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GivenOpeningDbConnectionThrowsException_WhenHealthCheck_ThenHealthCheckResultIsFalse()
    {
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        var result = await _context.Subject.HealthCheck();
        result.Should().BeFalse();
    }
}
