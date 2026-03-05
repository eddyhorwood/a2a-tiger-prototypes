using System.Data;
using FluentAssertions;
using Moq;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_CountPaymentTransactionsByOrganisationId
{
    private const string SearchPaymentTransactionByOrganisationIdSql = @"
            SELECT count(1) FROM payment_execution.paymenttransaction
            WHERE OrganisationId = @organisationId;
        ";

    private readonly PaymentTransactionDbCollection _context = new();

    [Fact]
    public async Task
        GivenOrgId_WhenCountPaymentTransactionsByOrgIdIsCalled_ThenDapperIsCalledWithCorrectQueryAndIntReturned()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var searchSql = SearchPaymentTransactionByOrganisationIdSql;
        _context.DapperWrapperMock.Setup(d =>
                d.QueryAsync<int>(It.IsAny<IDbConnection>(), searchSql,
                    It.Is<object>(obj => obj.ToString()!.Contains(organisationId.ToString()))))
            .ReturnsAsync([5])
            .Verifiable();

        // Act
        var numResults = await _context.Subject.CountPaymentTransactionsByOrganisationId(organisationId);

        // Assert
        numResults.Should().Be(5);
        _context.DapperWrapperMock.Verify();
    }

    [Fact]
    public async Task
        GivenOrgId_WhenCountPaymentTransactionsByOrgIdIsCalledAndDapperReturnsEmptyList_ThenIntZeroIsReturned()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var searchSql = SearchPaymentTransactionByOrganisationIdSql;
        _context.DapperWrapperMock.Setup(d =>
                d.QueryAsync<int>(It.IsAny<IDbConnection>(), searchSql,
                    It.Is<object>(obj => obj.ToString()!.Contains(organisationId.ToString()))))
            .ReturnsAsync([])
            .Verifiable();

        // Act
        var numResults = await _context.Subject.CountPaymentTransactionsByOrganisationId(organisationId);

        // Assert
        numResults.Should().Be(0);
        _context.DapperWrapperMock.Verify();
    }

    [Fact]
    public async Task
        GivenOrgId_WhenCountPaymentTransactionsByOrgIdIsCalledAndThrowsException_ThenItsAllowedToBubbleUp()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var searchSql = SearchPaymentTransactionByOrganisationIdSql;
        _context.DapperWrapperMock.Setup(d =>
                d.QueryAsync<int>(It.IsAny<IDbConnection>(), searchSql,
                    It.Is<object>(obj => obj.ToString()!.Contains(organisationId.ToString()))))
            .ThrowsAsync(new Exception("unique-ex"))
            .Verifiable();

        // Act + Assert
        await FluentActions.Awaiting(() => _context.Subject.CountPaymentTransactionsByOrganisationId(organisationId))
            .Should().ThrowAsync<Exception>().WithMessage("unique-ex");
        _context.DapperWrapperMock.Verify();
    }
}
