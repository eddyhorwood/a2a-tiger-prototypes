using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service.Strategies;
using PaymentExecution.Domain.Util;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.Service;

public class StripeIntegrationDomainService(
    StripeExecutionClient.Contracts.IStripeExecutionClient stripeExecutionClient,
    IMapper mapper,
    ILogger<StripeIntegrationDomainService> logger,
    IEnumerable<IStripePendingActionStrategy> strategies) : IProviderIntegrationDomainService
{
    public ProviderType ProviderType => ProviderType.Stripe;
    private readonly Dictionary<string, IStripePendingActionStrategy> _actionStrategies = strategies.ToDictionary(x => x.ActionType);

    private static readonly Dictionary<PaymentProviderStatus, List<string>> _statusMappings = new()
    {
        {
            PaymentProviderStatus.Submitted, ["requires_payment_method", "requires_confirmation"]
        },
        {
            PaymentProviderStatus.Processing, ["processing"]
        },
        {
            PaymentProviderStatus.RequiresAction, ["requires_action", "requires_capture"]
        },
        {
            PaymentProviderStatus.Terminal, ["succeeded", "canceled"]
        }
    };

    private static readonly Dictionary<string, PaymentMethodType> _paymentMethodTypeMap =
        Enum.GetValues<PaymentMethodType>()
            .ToDictionary(e =>
            {
                return EnumUtil.GetEnumMemberValue(e);
            }, e => e, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets provider state for payment request.
    /// When correlationId and tenantId are provided, headers are added explicitly (for Lambda).
    /// When not provided, relies on header propagation middleware (for API).
    /// </summary>
    public async Task<Result<ProviderState>> GetProviderStateAsync(Guid paymentRequestId, Guid? correlationId = null, Guid? tenantId = null)
    {
        var paymentIntentResult = await stripeExecutionClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);
        if (paymentIntentResult.IsFailed)
        {
            return paymentIntentResult.ToResult();
        }

        var stripePaymentIntentDto = paymentIntentResult.Value;

        var mappedStatusKvp = _statusMappings.FirstOrDefault(
            x => x.Value.Contains(stripePaymentIntentDto.Status));
        if (mappedStatusKvp.Value is null)
        {
            logger.LogError("The stripe status {StripeStatus} is not supported", stripePaymentIntentDto.Status);
            return Result.Fail("The stripe status is not supported");
        }

        var pendingDetailsResult = CreatePendingStatusDetails(stripePaymentIntentDto);
        if (pendingDetailsResult.IsFailed)
        {
            return pendingDetailsResult.ToResult();
        }

        var isFailedStatus = stripePaymentIntentDto.LastPaymentError != null && stripePaymentIntentDto.Status == "requires_payment_method";
        var paymentProviderStatus = isFailedStatus ? PaymentProviderStatus.Terminal : mappedStatusKvp.Key;

        return new ProviderState()
        {
            PaymentProviderStatus = paymentProviderStatus,
            PaymentProviderPaymentTransactionId = stripePaymentIntentDto.Id,
            ProviderType = ProviderType,
            LastPaymentErrorCode = stripePaymentIntentDto.LastPaymentError?.Code,
            PendingStatusDetails = pendingDetailsResult.Value
        };
    }

    public async Task<Result> CancelPaymentAsync(CancelPaymentRequest cancellationRequest)
    {
        var stripeExeCancelPaymentDto = mapper.Map<StripeExeCancelPaymentRequestDto>(cancellationRequest);
        var stripeCancellationResult = await stripeExecutionClient.CancelPaymentAsync(stripeExeCancelPaymentDto);
        return stripeCancellationResult;
    }

    private Result<PendingStatusDetails?> CreatePendingStatusDetails(StripeExePaymentIntentDto paymentIntentDto)
    {
        var nextActionDto = paymentIntentDto.NextAction;
        if (nextActionDto == null)
        {
            return Result.Ok<PendingStatusDetails?>(null);
        }

        if (paymentIntentDto.PaymentMethod == null || !_paymentMethodTypeMap.TryGetValue(paymentIntentDto.PaymentMethod.Type, out var paymentMethodType))
        {
            logger.LogError("Payment method is null or not supported. PaymentMethodType: {PaymentMethodType}", paymentIntentDto.PaymentMethod?.Type);
            return Result.Fail("Payment method must be provided and of a supported type");
        }

        if (!_actionStrategies.TryGetValue(nextActionDto.Type, out var strategy))
        {
            return new PendingStatusDetails()
            {
                PaymentMethodType = paymentMethodType,
                RequiresActionType = RequiresActionType.Unknown,
                HasActionValue = false
            };
        }

        var pendingDetailsResult = strategy.Map(paymentIntentDto, nextActionDto);
        if (pendingDetailsResult.IsFailed)
        {
            return pendingDetailsResult.ToResult();
        }

        pendingDetailsResult.Value.PaymentMethodType = paymentMethodType;

        return pendingDetailsResult.Value;
    }
}
