using System.ComponentModel;
using AutoMapper;
using PaymentExecution.Domain.Models;
using PaymentExecution.StripeExecutionClient.Contracts.Models;
using BillingContactDetails = PaymentExecution.Domain.Models.BillingContactDetails;
using LineItem = PaymentExecution.Domain.Models.LineItem;
using SelectedPaymentMethod = PaymentExecution.Domain.Models.SelectedPaymentMethod;
using SourceContext = PaymentExecution.Domain.Models.SourceContext;

namespace PaymentExecution.Domain.Mapping;

public class ServiceToDomainMappingProfile : Profile
{
    public ServiceToDomainMappingProfile()
    {
        // Nested type mappings
        CreateMap<PaymentRequestClient.Models.BillingContactDetails, BillingContactDetails>();
        CreateMap<PaymentRequestClient.Models.SelectedPaymentMethod, SelectedPaymentMethod>();
        CreateMap<PaymentRequestClient.Models.LineItem, LineItem>();
        CreateMap<PaymentRequestClient.Models.SourceContext, SourceContext>();
        CreateMap<PaymentRequestClient.Models.Receivable, Receivable>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src =>
                    ConvertEnumCaseInsensitive<ReceivableType>(src.Type.ToString())));

        CreateMap<PaymentRequestClient.Models.PaymentRequest, PaymentRequest>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src =>
                    ConvertEnumCaseInsensitive<RequestStatus>(src.Status.ToString())))
            .ForMember(dest => dest.Executor,
                opt => opt.MapFrom(src =>
                    ConvertEnumCaseInsensitive<ExecutorType>(src.Executor.ToString())));

        CreateMap<StripeExeSubmitPaymentResponseDto, SubmittedPayment>();
    }

    private static TEnum ConvertEnumCaseInsensitive<TEnum>(string value) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var result))
        {
            return result;
        }
        throw new InvalidEnumArgumentException($"'{value}' is not a valid value for enum '{typeof(TEnum).Name}'");
    }
}
