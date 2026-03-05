using AutoMapper;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Queries;
using PaymentExecution.Repository.Models;


namespace PaymentExecution.Domain.Mapping;

public class DomainToRepositoryMappingProfile : Profile
{
    public DomainToRepositoryMappingProfile()
    {
        CreateMap<PaymentTransactionDto, GetPaymentTransactionQueryResponse>();

        CreateMap<CompleteMessage, UpdateStatusPaymentTransactionDto>();

        CreateMap<CompleteMessage, UpdateSuccessPaymentTransactionDto>()
            .ForMember(dest => dest.EventCreatedDateTimeUtc, opt => opt.MapFrom(src => src.EventCreatedDateTime));

        CreateMap<CompleteMessage, UpdateFailurePaymentTransactionDto>()
            .ForMember(dest => dest.EventCreatedDateTimeUtc, opt => opt.MapFrom(src => src.EventCreatedDateTime));

        CreateMap<CompleteMessage, UpdateCancelledPaymentTransactionDto>()
            .ForMember(dest => dest.EventCreatedDateTimeUtc, opt => opt.MapFrom(src => src.EventCreatedDateTime));
    }

}
