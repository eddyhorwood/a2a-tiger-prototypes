using System.Data;
using FluentAssertions;
using Moq;

namespace PaymentExecution.Repository.UnitTests;

public class PaymentTransactionRepositoryTests_DeleteAllDataByOrganisationId
{
    private const string DeleteSql = @"
        with prs_deleted as (
          DELETE FROM payment_execution.paymenttransaction
          WHERE OrganisationId = @organisationId
          RETURNING *
        )
        SELECT count(1) FROM prs_deleted;";

    private readonly PaymentTransactionDbCollection _context = new();

    [Fact]
    public async Task
        GivenOrganisationId_WhenDeleteByOrganisationIdIsCalled_ThenDappersIsCalledWithCorrectSqlAndCompletedTaskReturned()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        _context.DapperWrapperMock.Setup(d =>
                d.QueryAsync<int>(It.IsAny<IDbConnection>(), DeleteSql,
                    It.Is<object>(obj => obj.ToString()!.Contains(organisationId.ToString()))))
            .ReturnsAsync([1])
            .Verifiable();

        // Act
        var prsDeleted = await _context.Subject.DeleteAllDataByOrganisationId(organisationId);

        // Assert
        _context.DapperWrapperMock.Verify();
        prsDeleted.Should().Be(1);
    }

    [Fact]
    public async Task
        GivenOrganisationId_WhenDeleteByOrgIdIsCalledAndThrowsException_ThenItsAllowedToBubbleUp()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        _context.DapperWrapperMock.Setup(d =>
                d.QueryAsync<int>(It.IsAny<IDbConnection>(), DeleteSql,
                    It.Is<object>(obj => obj.ToString()!.Contains(organisationId.ToString()))))
            .ThrowsAsync(new Exception("unique-ex"))
            .Verifiable();

        // Act + Assert
        await FluentActions.Awaiting(() => _context.Subject.DeleteAllDataByOrganisationId(organisationId))
            .Should().ThrowAsync<Exception>().WithMessage("unique-ex");
        _context.DapperWrapperMock.Verify();
    }
}
