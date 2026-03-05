using FluentResults;
using MediatR;
using PaymentExecution.Domain.Service;

namespace PaymentExecution.Domain.Commands;

public class RequestCancelCommand : IRequest<Result>
{
    public required Guid PaymentRequestId { get; set; }
    public required string CancellationReason { get; set; }
    public required Guid XeroTenantId { get; set; }
    public required Guid XeroCorrelationId { get; set; }
}


public class RequestCancelCommandHandler(IRequestCancelDomainService cancelDomainService) : IRequestHandler<RequestCancelCommand,
    Result>
{
    public async Task<Result> Handle(RequestCancelCommand request, CancellationToken cancellationToken)
    {
        var cancellationRequestResult = await cancelDomainService.HandleGetCancellationRequestAsync(request.PaymentRequestId);

        if (cancellationRequestResult.IsFailed)
        {
            return cancellationRequestResult.ToResult();
        }

        return await cancelDomainService.HandleRequestCancellationAsync(
            cancellationRequestResult.Value,
            request.CancellationReason,
            request.XeroTenantId,
            request.XeroCorrelationId);
    }
}
