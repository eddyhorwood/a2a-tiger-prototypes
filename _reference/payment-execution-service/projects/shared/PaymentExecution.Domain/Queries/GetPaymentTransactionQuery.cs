using AutoMapper;
using FluentResults;
using MediatR;
using PaymentExecution.Repository;
namespace PaymentExecution.Domain.Queries;

public class GetPaymentTransactionQueryResponse
{
    public Guid? PaymentTransactionId { get; set; }
    public required Guid PaymentRequestId { get; set; }
    public required string Status { get; set; }
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; }
    public string? PaymentProviderPaymentReferenceId { get; set; }
    public string? PaymentProviderPaymentTransactionId { get; set; }
    public string? FailureDetails { get; set; }
    public DateTime? EventCreatedDateTimeUtc { get; set; }
    public Guid? ProviderServiceId { get; set; }
    public required string ProviderType { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    public string? CancellationReason { get; set; }
}

public class GetPaymentTransactionQuery : IRequest<Result<GetPaymentTransactionQueryResponse>>
{
    public required Guid PaymentRequestId { get; set; }
}

public class GetPaymentTransactionQueryHandler(IPaymentTransactionRepository paymentTransactionRepository, IMapper mapper) : IRequestHandler<GetPaymentTransactionQuery, Result<GetPaymentTransactionQueryResponse>>
{
    public async Task<Result<GetPaymentTransactionQueryResponse>> Handle(
        GetPaymentTransactionQuery query, CancellationToken cancellationToken)
    {
        var paymentTransactionDtoResult = await paymentTransactionRepository.GetPaymentTransactionsByPaymentRequestId(query.PaymentRequestId);
        if (paymentTransactionDtoResult.IsFailed)
        {
            return paymentTransactionDtoResult.ToResult();
        }

        var paymentTransactionDto = paymentTransactionDtoResult.Value;
        var result = mapper.Map<GetPaymentTransactionQueryResponse>(paymentTransactionDto);

        return result;
    }
}
