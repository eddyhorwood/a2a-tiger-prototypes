using AutoMapper;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Mapping;

public class DomainToDomainMappingProfile : Profile
{
    public DomainToDomainMappingProfile()
    {
        CreateMap<CompleteMessageBody, CompleteMessage>()
            .ForMember(dest => dest.XeroCorrelationId, opt => opt.Ignore())
            .ForMember(dest => dest.MessageId, opt => opt.Ignore())
            .ForMember(dest => dest.ReceiptHandle, opt => opt.Ignore());

        CreateMap<ProcessCancelMessageCommand, CancelPaymentRequest>();

        CreateMap<ProcessCancelMessageCommand, GetProviderStateRequest>();

        CreateMap<SynchronousCancellationCommand, CancelPaymentRequest>();
    }
}
