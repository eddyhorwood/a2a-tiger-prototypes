using System.ComponentModel;
using AutoMapper;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Mapping;

public class RepositoryToDomainMappingProfile : Profile
{
    public RepositoryToDomainMappingProfile()
    {
        CreateMap<Repository.Models.PaymentTransactionDto, CancellationRequest>()
            .ForMember(m => m.Status,
                opt => opt.MapFrom(src =>
                    ConvertStringToEnumCaseInsensitive<TransactionStatus>(src.Status)))
            .ForMember(m => m.ProviderType, opt =>
                opt.MapFrom(src =>
                    ConvertStringToEnumCaseInsensitive<ProviderType>(src.ProviderType)));
    }

    private static TEnum ConvertStringToEnumCaseInsensitive<TEnum>(string value) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, true, out var result))
        {
            return result;
        }
        throw new InvalidEnumArgumentException($"'{value}' is not a valid value for enum '{typeof(TEnum).Name}'");
    }
}
