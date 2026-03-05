using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.StripeExecutionClient;

public class StripeExecutionClient(
    ILogger<StripeExecutionClient> logger,
    IStripeExecutionInternalHttpClient stripeExecutionInternalHttpClient
) : Contracts.IStripeExecutionClient
{
    private readonly JsonSerializerOptions _deserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<StripeExeSubmitPaymentResponseDto>> SubmitPaymentAsync(
        StripeExeSubmitPaymentRequestDto submitRequest)
    {
        var paymentRequestId = submitRequest.PaymentRequest.PaymentRequestId;
        logger.LogInformation("Submitting payment request to Stripe execution. PaymentRequestId: {PaymentRequestId}",
            paymentRequestId);

        var response = await stripeExecutionInternalHttpClient.SubmitStripeExecutionAsync(submitRequest);

        var responseStringContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var result = HandleErrorScenarios(response, responseStringContent, paymentRequestId);
            return result.ToResult();
        }

        var submitResponse = TryDeserializeResponseContent<StripeExeSubmitPaymentResponseDto>(responseStringContent, paymentRequestId);
        return submitResponse;
    }

    public async Task<Result> CancelPaymentAsync(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        var paymentRequestId = cancelRequest.PaymentRequestId;
        logger.LogInformation("Cancelling payment request with Stripe execution. PaymentRequestId: {PaymentRequestId}, CancellationReason: {CancellationReason}",
            paymentRequestId, cancelRequest.CancellationReason);

        var response = await stripeExecutionInternalHttpClient.CancelStripeExecutionAsync(cancelRequest);

        if (!response.IsSuccessStatusCode)
        {
            var responseStringContent = await response.Content.ReadAsStringAsync();
            return HandleErrorScenarios(response, responseStringContent, paymentRequestId).ToResult();
        }

        logger.LogInformation("Successfully cancelled payment request with Stripe execution. PaymentRequestId: {PaymentRequestId}",
            paymentRequestId);

        return Result.Ok();
    }

    public async Task<Result<StripeExePaymentIntentDto>> GetPaymentIntentByPaymentRequestIdAsync(
        Guid paymentRequestId,
        Guid? correlationId = null,
        Guid? tenantId = null)
    {
        var response = await stripeExecutionInternalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);
        var responseStringContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var paymentExecutionError = HandleErrorScenarios(response, responseStringContent, paymentRequestId);
            return paymentExecutionError.ToResult();
        }

        var paymentIntentDto = TryDeserializeResponseContent<StripeExePaymentIntentDto>(responseStringContent, paymentRequestId);

        return paymentIntentDto;
    }

    private T TryDeserializeResponseContent<T>(string responseStringContent, Guid paymentRequestId)
    {
        var deserializedResponse =
            JsonSerializer.Deserialize<T>(responseStringContent, _deserializerOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response content for Payment Request Id: {paymentRequestId}");
        return deserializedResponse;
    }

    private Result<PaymentExecutionError> HandleErrorScenarios(HttpResponseMessage response,
        string responseStringContent, Guid paymentRequestId)
    {
        logger.LogError(
            "Stripe Execution integration failed. Status code: {StatusCode}, Response: {ResponseContent}. PaymentRequestId: {PaymentRequestId}",
            response.StatusCode, responseStringContent, paymentRequestId);
        var deserializeResponseContentResult =
            ProblemDetailsExtended.TryDeserializeResponseContent(responseStringContent, _deserializerOptions);
        PaymentExecutionError stripeExecutionError;
        if (deserializeResponseContentResult.IsFailed)
        {
            stripeExecutionError =
                new PaymentExecutionError($"Failed Stripe Execution integration. {responseStringContent}", response.StatusCode);
        }
        else
        {
            var problemDetails = deserializeResponseContentResult.Value;

            stripeExecutionError = new PaymentExecutionError(
                problemDetails?.Detail ?? "Failed Stripe Execution integration",
                providerErrorCode: problemDetails?.ProviderErrorCode, response.StatusCode);
        }

        return Result.Fail(stripeExecutionError);
    }
}
