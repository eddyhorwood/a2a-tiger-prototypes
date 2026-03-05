using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using PaymentExecution.Domain.Models;
using PaymentExecutionService.Models.Response;

namespace PaymentExecutionService.Mapping;

public class DomainToControllerMappingProfile : Profile
{
    public DomainToControllerMappingProfile()
    {
        // Domain Model → API Response mappings
        CreateMap<RequiresActionType, RequiresActionTypeResponse>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();

        CreateMap<PaymentMethodType, PaymentMethodTypeResponse>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();

        CreateMap<PaymentProviderStatus, PaymentProviderStatusResponse>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();

        CreateMap<ProviderType, ProviderTypeResponse>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();

        CreateMap<ProviderState, GetProviderStateResponse>();

        CreateMap<PendingStatusDetails, PendingStatusDetailsResponse>();

        CreateMap<BankTransferInstructions, BankTransferInstructionsResponse>();
        CreateMap<FinancialAddresses, FinancialAddressesResponse>();
        CreateMap<Aba, AbaResponse>();
        CreateMap<Swift, SwiftResponse>();
        CreateMap<RedirectToUrl, RedirectToUrlResponse>();
        CreateMap<MicroDepositVerification, MicroDepositVerificationResponse>();
    }
}
