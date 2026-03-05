using System.Data;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using Npgsql;
using PaymentExecution.Repository.Models;
namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_UpdatePaymentTransactionStatusOnly
{
    private readonly PaymentTransactionDbCollection _context = new();
    private const string ExpectedQuery =
            @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        updatedUTC = @UpdatedUtc 
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

    [Theory, AutoData]
    public async Task GivenAnExceptionIsThrownWhenGettingAConnection_WhenUpdatePaymentTransactionStatusOnly_ThenReturnsResultFail(
        UpdateStatusPaymentTransactionDto mockUpdateDto)
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Throws(new Exception("oh dear!"));

        // Act
        var result = await _context.Subject.UpdatePaymentTransactionStatusOnly(mockUpdateDto);

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenABrokenDBConnect_WhenUpdatePaymentTransactionStatusOnly_ThenReturnResultFail(
        UpdateStatusPaymentTransactionDto mockUpdateDto)
    {

        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        //Act & Assert
        var result = await _context.Subject.UpdatePaymentTransactionStatusOnly(mockUpdateDto);

        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenExecutingQueryThrowsAnException_WhenUpdatePaymentTransactionStatusOnly_ThenReturnsResultFail(
        UpdateStatusPaymentTransactionDto mockUpdateDto)
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);
        _context.DapperWrapperMock.Setup(d => d.ExecuteAsync(
                _context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(), null))
            .ThrowsAsync(new NpgsqlException("Something happened!!"));

        // Act
        var result = await _context.Subject.UpdatePaymentTransactionStatusOnly(mockUpdateDto);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenAValidPaymentTransactionDto_WhenUpdatePaymentTransactionStatusOnly_ThenUpdateIsCalledSuccessfully()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var fakeUtcOffset = new DateTimeOffset(2024, 1, 9, 1, 0, 0, TimeSpan.Zero);
        _context.FakeTimeProvider.SetUtcNow(fakeUtcOffset);

        var paymentTransactionDto = new UpdateStatusPaymentTransactionDto
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderServiceId = Guid.NewGuid(),
            Status = "in_progress"
        };

        var expectedParam = new
        {
            PaymentRequestId = paymentTransactionDto.PaymentRequestId,
            ProviderServiceId = paymentTransactionDto.ProviderServiceId,
            Status = paymentTransactionDto.Status,
            UpdatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime
        };

        _context.DapperWrapperMock.Setup(d => d.ExecuteAsync(_context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(), null));

        // Act
        var result = await _context.Subject.UpdatePaymentTransactionStatusOnly(paymentTransactionDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.DapperWrapperMock.Verify(d => d.ExecuteAsync(
            It.IsAny<IDbConnection>(),
            It.Is<string>(sql => sql.GetHashCode().Equals(ExpectedQuery.GetHashCode())),
            It.Is<object>(p => p.GetHashCode().Equals(expectedParam.GetHashCode())),
            null
        ), Times.Once);
    }
}
