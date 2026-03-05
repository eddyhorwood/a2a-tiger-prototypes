using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models.Errors;
using PaymentExecution.Repository;
namespace PaymentExecution.Domain.UnitTests.Commands;

public class DeleteByOrgCommandTests
{
    private readonly Mock<IPaymentTransactionRepository> _repository = new();
    private readonly Mock<ILogger<DeleteByOrgCommandHandler>> _logger = new();

    [Fact]
    public async Task GivenDeleteCommand_WhenNoOrganisationDataFound_ThenPaymentTransactionNotFoundErrorReturned()
    {
        // Arrange
        var command = new DeleteByOrgCommand { OrganisationId = Guid.NewGuid() };
        _repository.Setup(r => r.CountPaymentTransactionsByOrganisationId(command.OrganisationId))
            .ReturnsAsync(0)
            .Verifiable();
        var handler = new DeleteByOrgCommandHandler(_repository.Object, _logger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<PaymentTransactionNotFoundError>().Should().BeTrue();
        _repository.Verify();
    }

    [Fact]
    public async Task
        GivenDeleteCommand_WhenTransactionsFoundAssociatedWithOrg_ThenDeleteMethodIsCalledAndOkResultCalled()
    {
        // Arrange
        var command = new DeleteByOrgCommand { OrganisationId = Guid.NewGuid() };
        _repository.Setup(r => r.CountPaymentTransactionsByOrganisationId(command.OrganisationId))
            .ReturnsAsync(6010)
            .Verifiable();
        _repository.Setup(r => r.DeleteAllDataByOrganisationId(command.OrganisationId))
            .ReturnsAsync(6010)
            .Verifiable();
        var handler = new DeleteByOrgCommandHandler(_repository.Object, _logger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Verify();
    }

    [Fact]
    public async Task GivenDeleteCommand_WhenRepoThrowsException_ThenItBubblesUp()
    {
        // Arrange
        var command = new DeleteByOrgCommand() { OrganisationId = Guid.NewGuid() };
        var expectedException = new Exception("Tremendous volcanic explosions sometimes occur.");
        _repository.Setup(r => r.CountPaymentTransactionsByOrganisationId(command.OrganisationId))
            .ThrowsAsync(expectedException)
            .Verifiable();
        var handler = new DeleteByOrgCommandHandler(_repository.Object, _logger.Object);

        // Act + Assert
        await FluentActions.Awaiting(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>().WithMessage(expectedException.Message);
    }
}
