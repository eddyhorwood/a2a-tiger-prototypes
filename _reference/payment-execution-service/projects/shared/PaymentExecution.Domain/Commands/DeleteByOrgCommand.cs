using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Models.Errors;
using PaymentExecution.Repository;

namespace PaymentExecution.Domain.Commands;

public record DeleteByOrgCommand : IRequest<Result>
{
    public Guid OrganisationId { get; set; }
}

public class DeleteByOrgCommandHandler(IPaymentTransactionRepository repository, ILogger<DeleteByOrgCommandHandler> logger)
    : IRequestHandler<DeleteByOrgCommand, Result>
{
    public async Task<Result> Handle(DeleteByOrgCommand command, CancellationToken cancellationToken)
    {
        var numberOfTransactionsAssociatedWithOrganisationId = await
            repository.CountPaymentTransactionsByOrganisationId(command.OrganisationId);
        if (numberOfTransactionsAssociatedWithOrganisationId == 0)
        {
            return Result.Fail(new PaymentTransactionNotFoundError());
        }

        var numPrs = await repository.DeleteAllDataByOrganisationId(command.OrganisationId);
        logger.LogInformation("Successfully deleted {NumPrs} payment transactions for organisation {OrganisationId}",
            numPrs, command.OrganisationId);
        return Result.Ok();
    }
}
