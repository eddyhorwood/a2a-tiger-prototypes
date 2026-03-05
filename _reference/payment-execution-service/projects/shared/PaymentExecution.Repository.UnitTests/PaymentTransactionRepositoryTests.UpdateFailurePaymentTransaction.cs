using System.Data;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using Npgsql;
using PaymentExecution.Repository.Models;
namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_UpdateFailurePaymentTransaction
{
    private readonly PaymentTransactionDbCollection _context = new();
    private readonly IFixture _fixture = new Fixture();
    private const string ExpectedQuery =
        @"UPDATE payment_execution.PaymentTransaction 
                    SET status = @Status, 
                        fee = @Fee, 
                        feeCurrency = @FeeCurrency, 
                        paymentProviderPaymentReferenceId = @PaymentProviderPaymentReferenceId, 
                        eventcreatedDateTimeUtc = @EventCreatedDateTimeUtc, 
                        failureDetails = @FailureDetails,
                        updatedUTC = @UpdatedUtc 
                    WHERE paymentRequestId = @PaymentRequestId AND providerServiceId = @ProviderServiceId";

    [Theory, AutoData]
    public async Task GivenAnExceptionIsThrownWhenGettingAConnection_WhenUpdateFailurePaymentTransactionData_ThenReturnsResultFail(
        UpdateFailurePaymentTransactionDto mockUpdateDto)
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Throws(new Exception("oh dear!"));

        // Act
        var result = await _context.Subject.UpdateFailurePaymentTransactionData(mockUpdateDto);

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenABrokenDBConnect_WhenUpdateFailurePaymentTransactionData_ThenReturnsResultFail(
        UpdateFailurePaymentTransactionDto mockUpdateDto)
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        //Act
        var result = await _context.Subject.UpdateFailurePaymentTransactionData(mockUpdateDto);

        result.IsFailed.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenExecutingQueryThrowsAnException_WhenUpdateFailurePaymentTransactionData_ThenReturnsResultFail(
        UpdateFailurePaymentTransactionDto mockUpdateDto)
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
        var result = await _context.Subject.UpdateFailurePaymentTransactionData(mockUpdateDto);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenAValidPaymentTransactionDto_WhenUpdateFailurePaymentTransactionData_ThenUpdateIsCalledSuccessfully()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var fakeUtcOffset = new DateTimeOffset(2024, 1, 9, 1, 0, 0, TimeSpan.Zero);
        _context.FakeTimeProvider.SetUtcNow(fakeUtcOffset);

        var paymentTransactionDto = _fixture.Create<UpdateFailurePaymentTransactionDto>();
        paymentTransactionDto.Status = "Failed";

        var expectedParam = new
        {
            PaymentRequestId = paymentTransactionDto.PaymentRequestId,
            ProviderServiceId = paymentTransactionDto.ProviderServiceId,
            Status = paymentTransactionDto.Status,
            Fee = paymentTransactionDto.Fee,
            FeeCurrency = paymentTransactionDto.FeeCurrency,
            PaymentProviderPaymentReferenceId = paymentTransactionDto.PaymentProviderPaymentReferenceId,
            FailureDetails = paymentTransactionDto.FailureDetails,
            EventCreatedDateTimeUtc = paymentTransactionDto.EventCreatedDateTimeUtc,
            UpdatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime
        };

        _context.DapperWrapperMock.Setup(d => d.ExecuteAsync(_context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(), null));

        // Act
        var result = await _context.Subject.UpdateFailurePaymentTransactionData(paymentTransactionDto);

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
