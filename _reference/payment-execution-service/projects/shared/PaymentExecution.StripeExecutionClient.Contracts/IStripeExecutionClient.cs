using FluentResults;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.StripeExecutionClient.Contracts;

public interface IStripeExecutionClient
{
    /// <summary>
    /// Gets payment intent by payment request ID.
    /// When correlationId and tenantId are provided, headers are added explicitly (for Lambda).
    /// When not provided, relies on header propagation middleware (for API).
    /// </summary>
    Task<Result<StripeExePaymentIntentDto>> GetPaymentIntentByPaymentRequestIdAsync(
        Guid paymentRequestId,
        Guid? correlationId = null,
        Guid? tenantId = null);

    Task<Result> CancelPaymentAsync(StripeExeCancelPaymentRequestDto cancelRequest);

    Task<Result<StripeExeSubmitPaymentResponseDto>> SubmitPaymentAsync(StripeExeSubmitPaymentRequestDto submitRequest);
}
