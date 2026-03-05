using AutoMapper;
using PaymentExecution.Domain.Models;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.Mapping;

public class DomainToServiceMappingProfile : Profile
{
    public DomainToServiceMappingProfile()
    {
        CreateMap<CompleteMessage, SuccessPaymentRequest>()
                .ForMember(dest => dest.PaymentCompletionDateTime, opt => opt.MapFrom(src => src.EventCreatedDateTime));

        CreateMap<CompleteMessage, FailurePaymentRequest>()
            .ForMember(dest => dest.PaymentCompletionDateTime, opt => opt.MapFrom(src => src.EventCreatedDateTime));

        CreateMap<CompleteMessage, PaymentRequestClient.Models.Requests.CancelPaymentRequest>();

        CreateMap<Models.CancelPaymentRequest, StripeExeCancelPaymentRequestDto>();

        // Domain Payment Request -> StripeExe Payment Request
        CreateMap<BillingContactDetails, StripeExeBillingContactDetailsDto>();
        CreateMap<SelectedPaymentMethod, StripeExeSelectedPaymentMethodDto>();
        CreateMap<LineItem, StripeExeLineItemDto>();
        CreateMap<SourceContext, StripeExeSourceContextDto>();

        CreateMap<PaymentRequest, StripeExePaymentRequestDto>()
            .ForMember(dest => dest.Executor, opt => opt.MapFrom(src => src.Executor.ToString()));
    }
}
