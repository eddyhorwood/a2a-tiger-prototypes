using System.Data;
using PaymentExecution.Common;

namespace PaymentExecution.Repository.UnitTests;

using Moq;
using Xunit;

public class SetPaymentTransactionFailedTests
{
    private readonly PaymentTransactionDbCollection _context = new();

    private const string SetPaymentTransactionFailedSql =
        @"UPDATE payment_execution.PaymentTransaction SET status = @Status, failureDetails = @FailureDetails, updatedUTC = @UpdatedUtc WHERE paymentRequestId = @PaymentRequestId";

    [Fact]
    public async Task GivenValidInput_WhenSetPaymentTransactionFailed_ThenReturnSuccess()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var paymentRequestId = Guid.NewGuid();
        var failureDetails = "Failure reason";
        var failedStatus = "anystatus";

        _context.DapperWrapperMock
            .Setup(wrapper => wrapper.ExecuteAsync(_context.DbConnectionMock.Object, SetPaymentTransactionFailedSql,
                It.IsAny<object>(), null))
            .ReturnsAsync(1);

        // Act
        var result = await _context.Subject.SetPaymentTransactionFailed(paymentRequestId, failureDetails, failedStatus);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenDatabaseError_WhenSetPaymentTransactionFailed_ThenReturnFailure()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        _context.DapperWrapperMock
            .Setup(wrapper => wrapper.ExecuteAsync(_context.DbConnectionMock.Object, SetPaymentTransactionFailedSql,
                It.IsAny<object>(), null))
            .ThrowsAsync(new Exception("Database error"));
        var paymentRequestId = Guid.NewGuid();
        var failureDetails = "Failure reason";
        var failedStatus = "failedstatus"; // make sure whatever string will be passed as param

        // Act
        var result = await _context.Subject.SetPaymentTransactionFailed(paymentRequestId, failureDetails, failedStatus);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(PaymentTransactionRepository.SetPaymentTransactionFailedErrorMessage,
            result.GetFirstErrorMessage());
        _context.DapperWrapperMock.Verify(m => m.ExecuteAsync(_context.DbConnectionMock.Object,
            It.Is<string>(sql => sql == SetPaymentTransactionFailedSql),
            It.Is<object>(obj =>
                obj.GetType().GetProperty("FailureDetails")!.GetValue(obj) as string == failureDetails &&
                obj.GetType().GetProperty("Status")!.GetValue(obj) as string == failedStatus),
            It.IsAny<IDbTransaction>()), Times.Once);
    }

    [Fact]
    public async Task GivenProviderStringExceeds125Characters_WhenSetPaymentTransactionFailed_ThenStringTruncated()
    {
        // Arrange
        var initialString =
            "this is going to be over 125 characters! This is a long string, so we need to make sure we truncate it to avoid any" +
            "sql exception due to exceeding of the character limit";
        var expectedTruncatedString = initialString.Substring(0, 125);

        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        _context.DapperWrapperMock
            .Setup(wrapper => wrapper.ExecuteAsync(_context.DbConnectionMock.Object, It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>()))
            .ReturnsAsync(1);
        var failedStatus = "Failed";

        // Act
        var result = await _context.Subject.SetPaymentTransactionFailed(Guid.NewGuid(), initialString, failedStatus);

        // Assert truncated string was used in the call
        Assert.True(result.IsSuccess);
        _context.DapperWrapperMock.Verify(m => m.ExecuteAsync(_context.DbConnectionMock.Object, It.IsAny<string>(),
            It.Is<object>(obj =>
                obj.GetType().GetProperty("FailureDetails")!.GetValue(obj) as string == expectedTruncatedString &&
                obj.GetType().GetProperty("Status")!.GetValue(obj) as string == failedStatus),
            It.IsAny<IDbTransaction>()), Times.Once);
    }
}
