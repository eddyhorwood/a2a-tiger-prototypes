using System.Data;
using FluentAssertions;
using Moq;
using Npgsql;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_GetPaymentTransactionsByPaymentRequestId
{
    private readonly PaymentTransactionDbCollection _context = new();

    private const string ExpectedQuery =
        @"SELECT 
                   PaymentTransactionId, 
                   PaymentRequestId, 
                   ProviderServiceId, 
                   Status, 
                   Fee, 
                   FeeCurrency, 
                   PaymentProviderPaymentReferenceId, 
                   PaymentProviderPaymentTransactionId, 
                   FailureDetails, 
                   EventCreatedDateTimeUtc, 
                   ProviderType, 
                   CreatedUtc, 
                   UpdatedUtc,
                   CancellationReason FROM payment_execution.PaymentTransaction 
                  WHERE paymentRequestId = @PaymentRequestId";

    [Fact]
    public async Task GivenAnExceptionIsThrownWhenGettingAConnection_WhenGetPaymentTransactionByPaymentRequestId_ThenReturnsResultFail()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Throws(new Exception("oh dear!"));

        // Act
        var result = await _context.Subject.GetPaymentTransactionsByPaymentRequestId(Guid.NewGuid());

        //Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenABrokenDBConnect_WhenGetPaymentTransactionByPaymentRequestId_ThenReturnsResultFail()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        //Act & Assert
        var result = await _context.Subject.GetPaymentTransactionsByPaymentRequestId(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenTheQueryThrowsAnException_WhenGetPaymentTransactionByPaymentRequestId_ThenReturnsResultFail()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);
        _context.DapperWrapperMock.Setup(d => d.QueryFirstOrDefaultAsync<PaymentTransactionDto>(
                _context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new NpgsqlException("Something happened!!"));

        // Act
        var result = await _context.Subject.GetPaymentTransactionsByPaymentRequestId(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GivenAValidPaymentRequestId_WhenGetPaymentTransactionByPaymentRequestId_ThenResultOkAndTransactionData()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var fakeUtcOffset = new DateTimeOffset(2024, 1, 9, 1, 0, 0, TimeSpan.Zero);
        _context.FakeTimeProvider.SetUtcNow(fakeUtcOffset);

        var paymentRequestId = Guid.NewGuid();

        var paymentTransactionDto = new PaymentTransactionDto
        {
            PaymentRequestId = paymentRequestId,
            ProviderServiceId = Guid.NewGuid(),
            Fee = 0.0m,
            Status = "in_progress",
            CreatedUtc = new DateTime(2024, 1, 1),
            UpdatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime,
            ProviderType = ProviderType.Stripe.ToString()
        };
        var expectedParam = new
        {
            PaymentRequestId = paymentRequestId
        };

        _context.DapperWrapperMock.Setup(d => d.QueryFirstOrDefaultAsync<PaymentTransactionDto>(_context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(paymentTransactionDto);

        // Act
        var actualPaymentTransactionDtoResult = await _context.Subject.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);

        // Assert
        actualPaymentTransactionDtoResult.IsSuccess.Should().BeTrue();

        var actualPaymentTransactionDto = actualPaymentTransactionDtoResult.Value;
        _context.DapperWrapperMock.Verify(d => d.QueryFirstOrDefaultAsync<PaymentTransactionDto>(
            It.IsAny<IDbConnection>(),
            It.Is<string>(sql => sql.GetHashCode().Equals(ExpectedQuery.GetHashCode())),
            It.Is<object>(p => p.GetHashCode().Equals(expectedParam.GetHashCode()))
        ), Times.Once);

        actualPaymentTransactionDto.Should().NotBeNull();
        actualPaymentTransactionDto.Should().BeOfType<PaymentTransactionDto>();
        actualPaymentTransactionDto.Should().BeEquivalentTo(paymentTransactionDto);
    }
}
