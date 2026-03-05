using AutoMapper;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Models;

namespace PaymentExecutionService.Mapping;

public class ControllerToDomainMappingProfile : Profile
{
    private const int MaxCancellationReasonLength = 125;

    public ControllerToDomainMappingProfile()
    {
        CreateMap<CompletePaymentTransactionRequest, ExecutionQueueMessage>()
            .ForMember(executionQueueMessage => executionQueueMessage.ProviderType, opt
                => opt.MapFrom(req => req.ProviderType.ToString()))
            .ForMember(executionQueueMessage => executionQueueMessage.Status, opt
                => opt.MapFrom(req => req.Status.ToString()));

        CreateMap<SubmitStripeRequest, SubmitStripePaymentCommand>()
            .ForMember(dest => dest.XeroCorrelationId, opt => opt.Ignore())
            .ForMember(dest => dest.XeroTenantId, opt => opt.Ignore());

        CreateMap<RequestCancelPayload, RequestCancelCommand>()
            .ForMember(dest => dest.CancellationReason,
                opt => opt.MapFrom(src => TruncateString(src.CancellationReason, MaxCancellationReasonLength)))
            .ForMember(dest => dest.PaymentRequestId, opt => opt.Ignore())
            .ForMember(dest => dest.XeroCorrelationId, opt => opt.Ignore())
            .ForMember(dest => dest.XeroTenantId, opt => opt.Ignore());
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (value.Length > maxLength)
        {
            return value.Substring(0, maxLength);
        }

        return value;
    }
}
