using FluentResults;
using MediatR;
using PaymentExecution.Repository;

namespace PaymentExecution.Domain.Commands;

public class DBHealthCheckCommand : IRequest<Result<bool>>
{
}

public class DBHealthCheckCommandHandler(IPaymentTransactionRepository repository)
    : IRequestHandler<DBHealthCheckCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DBHealthCheckCommand request, CancellationToken cancellationToken)
    {
        var isHealthy = await repository.HealthCheck();
        return Result.Ok(isHealthy);
    }
}
