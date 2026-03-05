using AutoMapper;
using PaymentExecution.Domain.Commands;
using PaymentExecutionLambda.CancelLambda.Models;

namespace PaymentExecutionLambda.CancelLambda.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Map CancelPaymentMessage to ProcessCancelMessageCommand
        // Flatten nested CancelPaymentRequest properties
        CreateMap<CancelPaymentMessage, ProcessCancelMessageCommand>()
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.CorrelationId))
            .ForMember(dest => dest.PaymentRequestId, opt => opt.MapFrom(src => src.CancelPaymentRequest.PaymentRequestId))
            .ForMember(dest => dest.ProviderType, opt => opt.MapFrom(src => src.CancelPaymentRequest.ProviderType))
            .ForMember(dest => dest.CancellationReason, opt => opt.MapFrom(src => src.CancelPaymentRequest.CancellationReason));
    }
}

