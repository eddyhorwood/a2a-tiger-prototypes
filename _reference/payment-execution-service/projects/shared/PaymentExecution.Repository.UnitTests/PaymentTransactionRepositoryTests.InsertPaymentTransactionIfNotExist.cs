using System.Data;
using FluentAssertions;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;
namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_InsertPaymentTransactionIfNotExist
{
    private readonly PaymentTransactionDbCollection _context = new();

    [Fact]
    public async Task GivenABrokenDBConnect_WhenInsertPaymentTransactionIfNotExist_ThenReturnFailedResult()
    {

        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        var paymentTransactionDto = new InsertPaymentTransactionDto()
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            Status = "in_progress",
            OrganisationId = Guid.NewGuid()
        };

        //Act
        var result = await _context.Subject.InsertPaymentTransactionIfNotExist(paymentTransactionDto);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Message.Should().Be(PaymentTransactionRepository.InsertPaymentTransactionErrorMessage);
    }

    [Fact]
    public async Task GivenAValidPaymentTransaction_WhenInsertPaymentTransactionIfNotExist_ThenDataInserted()
    {

        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var fakeUtcOffset = new DateTimeOffset(2024, 1, 9, 1, 0, 0, TimeSpan.Zero);
        _context.FakeTimeProvider.SetUtcNow(fakeUtcOffset);
        var organisationId = Guid.NewGuid();
        var paymentTransactionDto = new InsertPaymentTransactionDto()
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = ProviderType.Stripe.ToString(),
            Status = "in_progress",
            OrganisationId = organisationId
        };


        var expectedQuery = @"
                WITH ins AS (
                    INSERT INTO payment_execution.PaymentTransaction (paymentRequestId, status, providerType, createdUTC, updatedUTC, organisationId) 
                    VALUES (@PaymentRequestId, @Status, @ProviderType, @CreatedUtc, @UpdatedUtc, @OrganisationId)
                    ON CONFLICT (PaymentRequestId) DO NOTHING
                    RETURNING paymentTransactionId
                )
                SELECT paymentTransactionId
                FROM ins
                UNION ALL
                SELECT paymentTransactionId
                FROM payment_execution.PaymentTransaction 
                WHERE PaymentRequestId = @PaymentRequestId
                LIMIT 1;
                ";
        var expectedParam = new
        {
            PaymentRequestId = paymentTransactionDto.PaymentRequestId,
            Status = paymentTransactionDto.Status,
            ProviderType = paymentTransactionDto.ProviderType,
            CreatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime,
            UpdatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime,
            OrganisationId = organisationId
        };
        var paymentTransactionId = Guid.NewGuid();

        _context.DapperWrapperMock.Setup(d => d.ExecuteScalarAsync<Guid>(_context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(), _context.TransactionMock.Object))
            .ReturnsAsync(paymentTransactionId);

        //Act 
        var generatedPaymentTransactionId = await _context.Subject.InsertPaymentTransactionIfNotExist(paymentTransactionDto);

        //Assert
        generatedPaymentTransactionId.IsSuccess.Should().BeTrue();
        Assert.Equal(paymentTransactionId, generatedPaymentTransactionId.Value);

        _context.DapperWrapperMock.Verify(d => d.ExecuteScalarAsync<Guid>(
            It.IsAny<IDbConnection>(),
            It.Is<string>(sql => sql.GetHashCode().Equals(expectedQuery.GetHashCode())),
            It.Is<object>(p => p.GetHashCode().Equals(expectedParam.GetHashCode())),
            It.IsAny<IDbTransaction>()
        ), Times.Once);
    }
}
