using FluentResults;
using MediatR;
using PaymentExecution.Common;
using PaymentExecution.Domain.Service;
using PaymentExecution.FeatureFlagClient;

namespace PaymentExecution.Domain.Commands;

public class SubmitStripePaymentCommand : IRequest<Result<SubmitStripePaymentCommandResponse>>
{
    public required Guid PaymentRequestId { get; set; }
    public List<string>? PaymentMethodsMadeAvailable { get; set; }
    public string? PaymentMethodId { get; set; }
    public required string XeroCorrelationId { get; set; }
    public required string XeroTenantId { get; set; }
}

public class SubmitStripePaymentCommandResponse
{
    public required string PaymentIntentId { get; set; }
    public required string ClientSecret { get; set; }
}


public class SubmitStripePaymentCommandHandler(
    ISubmitStripePaymentDomainService domainService,
    IFeatureFlagClient featureFlagClient)
    : IRequestHandler<SubmitStripePaymentCommand, Result<SubmitStripePaymentCommandResponse>>
{
    public async Task<Result<SubmitStripePaymentCommandResponse>> Handle(
        SubmitStripePaymentCommand submitStripeCommand,
        CancellationToken cancellationToken)
    {
        var paymentRequestId = submitStripeCommand.PaymentRequestId;
        var submitPaymentRequestResult = await domainService.SubmitToPaymentRequestAsync(paymentRequestId);

        if (submitPaymentRequestResult.IsFailed)
        {
            return submitPaymentRequestResult.ToResult();
        }

        var paymentRequest = submitPaymentRequestResult.Value;

        var insertPaymentTransactionResult =
            await domainService.CreatePaymentTransactionWithCompensationActionAsync(paymentRequestId, paymentRequest.OrganisationId);
        if (insertPaymentTransactionResult.IsFailed)
        {
            return insertPaymentTransactionResult.ToResult();
        }

        var submitStripeExecutionResult = await domainService.SubmitRequestToStripeExecutionWithCompensationActionAsync(paymentRequest,
            submitStripeCommand.PaymentMethodsMadeAvailable, submitStripeCommand.PaymentMethodId);
        if (submitStripeExecutionResult.IsFailed)
        {
            return submitStripeExecutionResult.ToResult();
        }

        var submittedPayment = submitStripeExecutionResult.Value;

        var sendMessageToQueueFeatureFlag = featureFlagClient.GetFeatureFlag(ExecutionConstants.FeatureFlags.SendMessageToCancelExecutionQueue);

        if (sendMessageToQueueFeatureFlag.Value)
        {
            // New behavior: update payment transaction and send message to cancel execution queue
            var updatePaymentTransaction = domainService.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
                submittedPayment.PaymentIntentId,
                insertPaymentTransactionResult.Value,
                submittedPayment.ProviderServiceId,
                paymentRequestId);

            var sendCancelMessage = domainService.TryToSendMessageToCancelExecutionQueueAsync(
                paymentRequestId,
                submittedPayment.TtlInSeconds,
                submitStripeCommand.XeroCorrelationId,
                submitStripeCommand.XeroTenantId);

            await Task.WhenAll(updatePaymentTransaction, sendCancelMessage);
        }
        else
        {
            // Old behavior: just update payment transaction
            await domainService.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
                submittedPayment.PaymentIntentId, insertPaymentTransactionResult.Value,
                submittedPayment.ProviderServiceId, paymentRequestId);
        }

        return Result.Ok(new SubmitStripePaymentCommandResponse()
        {
            ClientSecret = submittedPayment.ClientSecret,
            PaymentIntentId = submittedPayment.PaymentIntentId
        });
    }

}
