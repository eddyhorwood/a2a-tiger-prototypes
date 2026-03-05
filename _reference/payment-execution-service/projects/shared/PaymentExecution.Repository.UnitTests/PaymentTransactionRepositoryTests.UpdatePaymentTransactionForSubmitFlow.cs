using System.Data;
using AutoFixture;
using FluentAssertions;
using Moq;
using PaymentExecution.Repository.Models;
namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_UpdatePaymentTransactionForSubmitFlow
{
    private readonly PaymentTransactionDbCollection _context = new();
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task GivenABrokenDBConnect_WhenUpdatePaymentTransactionForSubmitFlowData_ThenReturnFailedResult()
    {

        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName).Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection()).Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(m => m.Open()).Throws(new Exception());

        var paymentTransactionDto = new UpdateForSubmitFlowDto()
        {
            PaymentTransactionId = Guid.NewGuid(),
            ProviderServiceId = Guid.NewGuid(),
            PaymentProviderPaymentTransactionId = "pi_1234"
        };

        //Act
        var result = await _context.Subject.UpdatePaymentTransactionWithProviderDetails(paymentTransactionDto);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors.First().Message.Should().Be(PaymentTransactionRepository.UpdatePaymentTransactionErrorMessage);

    }

    [Fact]
    public async Task GivenAValidPaymentTransactionDto_WhenUpdatePaymentTransactionForSubmitFlowData_ThenUpdateIsCalledSuccessfully()
    {
        // Arrange
        _context.PaymentExecutionDbConnectionMock.Setup(connection => connection.DatabaseName)
            .Returns("PaymentExecutionDB");
        _context.PaymentExecutionDbConnectionMock.Setup(m => m.GetConnection())
            .Returns(_context.DbConnectionMock.Object);
        _context.DbConnectionMock.Setup(d => d.BeginTransaction()).Returns(_context.TransactionMock.Object);

        var fakeUtcOffset = new DateTimeOffset(2024, 1, 9, 1, 0, 0, TimeSpan.Zero);
        _context.FakeTimeProvider.SetUtcNow(fakeUtcOffset);

        var paymentTransactionDto = _fixture.Create<UpdateForSubmitFlowDto>();

        var expectedQuery = @"UPDATE payment_execution.PaymentTransaction SET paymentProviderPaymentTransactionId = @PaymentProviderPaymentTransactionId, providerServiceId = @ProviderServiceId, updatedUTC = @UpdatedUtc WHERE paymentTransactionId = @PaymentTransactionId";

        var expectedParam = new
        {
            PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId,
            ProviderServiceId = paymentTransactionDto.ProviderServiceId,
            PaymentTransactionId = paymentTransactionDto.PaymentTransactionId,
            UpdatedUtc = _context.FakeTimeProvider.GetUtcNow().DateTime
        };

        _context.DapperWrapperMock.Setup(d => d.ExecuteAsync(_context.DbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(), null));

        // Act
        await _context.Subject.UpdatePaymentTransactionWithProviderDetails(paymentTransactionDto);

        // Assert
        _context.DapperWrapperMock.Verify(d => d.ExecuteAsync(
            It.IsAny<IDbConnection>(),
            It.Is<string>(sql => sql.GetHashCode().Equals(expectedQuery.GetHashCode())),
            It.Is<object>(p => p.GetHashCode().Equals(expectedParam.GetHashCode())),
            null
        ), Times.Once);

    }
}
