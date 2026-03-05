using FluentResults;
using MediatR;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;

namespace PaymentExecution.Domain.Queries;

public class GetProviderStateQuery : IRequest<Result<GetProviderStateQueryResponse>>
{
    public required Guid PaymentRequestId { get; init; }
}

public class GetProviderStateQueryResponse
{
    public required ProviderState ProviderState { get; set; }
}

public class GetProviderStateQueryHandler(
    IGetProviderStateDomainService getProviderStateDomainService)
    : IRequestHandler<GetProviderStateQuery, Result<GetProviderStateQueryResponse>>
{
    public async Task<Result<GetProviderStateQueryResponse>> Handle(
        GetProviderStateQuery request,
        CancellationToken cancellationToken)
    {
        var paymentTransactionResult = await getProviderStateDomainService.HandleGetPaymentTransactionById(request.PaymentRequestId);
        if (paymentTransactionResult.IsFailed)
        {
            return paymentTransactionResult.ToResult();
        }

        var getProviderStateResult = await getProviderStateDomainService.HandleGetProviderStateAsync(
            paymentTransactionResult.Value.ProviderType,
            request.PaymentRequestId);

        if (getProviderStateResult.IsFailed)
        {
            return getProviderStateResult.ToResult();
        }

        return Result.Ok(new GetProviderStateQueryResponse()
        {
            ProviderState = getProviderStateResult.Value
        });
    }
}
