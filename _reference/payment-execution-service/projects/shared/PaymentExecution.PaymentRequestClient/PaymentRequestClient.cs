using System.Text;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentExecution.Common;
using PaymentExecution.Common.Models;
using PaymentExecution.PaymentRequestClient.Exception;
using PaymentExecution.PaymentRequestClient.Models;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using Polly.Registry;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PaymentExecution.PaymentRequestClient;
public interface IPaymentRequestClient
{
    Task<Result<PaymentRequest>> SubmitPaymentRequest(Guid paymentRequestId);
    Task<Result> ExecutionSucceedPaymentRequest(Guid paymentRequestId, SuccessPaymentRequest paymentRequest, string xeroCorrelationId, string xeroTenantId);
    Task<Result> FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest, string xeroCorrelationId, string xeroTenantId);
    Task FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest);
    Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken);
    Task<Result<PaymentRequest>> GetPaymentRequestByPaymentRequestId(Guid paymentRequestId, string xeroCorrelationId);

    Task<Result> CancelPaymentRequest(Guid paymentRequestId, CancelPaymentRequest cancelPaymentRequest, string xeroCorrelationId, string xeroTenantId);
}

public class PaymentRequestClient(
    HttpClient httpClient,
    ILogger<PaymentRequestClient> logger,
    IOptions<PaymentRequestServiceOptions> paymentRequestOptions,
    ResiliencePipelineProvider<string> pipelineProvider) : IPaymentRequestClient
{
    private const string requestIdPlaceHolder = "{request-id}";

    private readonly JsonSerializerOptions _deserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<PaymentRequest>> SubmitPaymentRequest(Guid paymentRequestId)
    {
        logger.LogInformation("Submitting payment request with ID: {PaymentRequestId}", paymentRequestId);

        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var submitUrl = paymentRequestOptions.Value.SubmitPaymentRequestEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(submitUrl, UriKind.Relative)
            };
            return await httpClient.SendAsync(request, cancellationToken);
        });
        var responseContent = await response.Content.ReadAsStringAsync();

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            //Convert the response content to ProblemDetailsExtended to extract error code and message
            logger.LogError(
                "Submit payment request failed with status code: {StatusCode} and content: {ResponseContent}. PaymentRequestId: {PaymentRequestId}",
                response.StatusCode, responseContent, paymentRequestId);
            return Result.Fail(new PaymentExecutionError(ErrorMessage.SubmitPaymentRequestBadRequest));
        }

        response.EnsureSuccessStatusCode();

        return TryDeserializeResponseContent(responseContent, paymentRequestId);
    }

    public async Task<Result> ExecutionSucceedPaymentRequest(Guid paymentRequestId, SuccessPaymentRequest paymentRequest, string xeroCorrelationId, string xeroTenantId)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var successUrl = paymentRequestOptions.Value.ExecutionSuccessPaymentRequestEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(successUrl, UriKind.Relative),
                Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, ExecutionConstants.ApplicationJsonMediaType),
                Headers =
                {
                    { "Xero-Correlation-Id", xeroCorrelationId },
                    { "Xero-Tenant-Id", xeroTenantId }
                }
            };

            return await httpClient.SendAsync(request, cancellationToken);
        });

        if (!response.IsSuccessStatusCode)
        {
            var executionSuccessRequestResponseBody = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to execution succeed payment request for paymentRequestId: {PaymentRequestId}. Response: {ResponseBody}",
                paymentRequestId, executionSuccessRequestResponseBody);
            var paymentExecutionError = new PaymentExecutionError("Failed to succeed payment request.", response.StatusCode);
            return Result.Fail(paymentExecutionError);
        }

        logger.LogInformation("Successfully succeeded payment request for paymentRequestId: {PaymentRequestId}", paymentRequestId);
        return Result.Ok();
    }


    public async Task FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest)
    {
        try
        {
            var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));

            var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
            {

                var failureUrl = paymentRequestOptions.Value.FailurePaymentRequestEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(failureUrl, UriKind.Relative),
                    Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, ExecutionConstants.ApplicationJsonMediaType),
                };

                return await httpClient.SendAsync(request, cancellationToken);
            });

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                logger.LogError("problem details : {Problem} ", await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();
        }
        catch (System.Exception ex)
        {
            //Log used for crucial monitoring - do not change
            var redactedException = new RedactedException(ex.Message, ExceptionType.PaymentRequestException);
            logger.LogError(redactedException,
                "Failed to fail payment request for PaymentRequestId: {PaymentRequestId}", paymentRequestId);
            throw new PaymentRequestException(redactedException.Message, redactedException);
        }
    }

    public async Task<Result> FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest, string xeroCorrelationId, string xeroTenantId)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));
        try
        {
            var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
            {
                var failureUrl = paymentRequestOptions.Value.FailurePaymentRequestEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(failureUrl, UriKind.Relative),
                    Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, ExecutionConstants.ApplicationJsonMediaType),
                    Headers =
                    {
                        { "Xero-Correlation-Id", xeroCorrelationId },
                        { "Xero-Tenant-Id", xeroTenantId }
                    }
                };

                return await httpClient.SendAsync(request, cancellationToken);
            });

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                logger.LogError("problem details : {Problem} ", await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();
            return Result.Ok();
        }
        catch (System.Exception ex)
        {
            var redactedException = new RedactedException(ex.Message, ExceptionType.PaymentRequestException);
            logger.LogError(redactedException,
                "Failed to fail payment request for paymentRequestId: {PaymentRequestId}, Xero-Correlation-Id: {XeroCorrelationId}",
                paymentRequestId, xeroCorrelationId);
            return Result.Fail("Failed to fail payment request");
        }
    }

    public async Task<Result> CancelPaymentRequest(Guid paymentRequestId, CancelPaymentRequest cancelPaymentRequest, string xeroCorrelationId, string xeroTenantId)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));
        try
        {
            var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
            {
                var cancelExecutionInProgressUrl = paymentRequestOptions.Value.CancelExecutionInProgressPaymentRequestEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(cancelExecutionInProgressUrl, UriKind.Relative),
                    Content = new StringContent(JsonSerializer.Serialize(cancelPaymentRequest), Encoding.UTF8, ExecutionConstants.ApplicationJsonMediaType),
                    Headers =
                    {
                        {
                            "Xero-Correlation-Id", xeroCorrelationId
                        },
                        {
                            "Xero-Tenant-Id", xeroTenantId
                        }
                    }
                };

                return await httpClient.SendAsync(request, cancellationToken);
            });
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                logger.LogError("problem details : {Problem} ", await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();
            return Result.Ok();
        }
        catch (System.Exception ex)
        {
            var cancelPaymentRequestRedactedException = new RedactedException(ex.Message, ExceptionType.PaymentRequestException);
            logger.LogError(cancelPaymentRequestRedactedException,
                "Failed to cancel payment request for paymentRequestId: {PaymentRequestId}, Xero-Correlation-Id: {XeroCorrelationId}",
                paymentRequestId, xeroCorrelationId);
            return Result.Fail("Failed to cancel payment request");
        }
    }

    public async Task<Result<PaymentRequest>> GetPaymentRequestByPaymentRequestId(Guid paymentRequestId, string xeroCorrelationId)
    {
        var resiliencePipeline = pipelineProvider.GetPipeline(nameof(PaymentRequestClient));
        var response = await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            var getUrl = paymentRequestOptions.Value.GetPaymentRequestByIdEndpoint.Replace(requestIdPlaceHolder, paymentRequestId.ToString());
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(getUrl, UriKind.Relative),
                Headers =
                {
                    { ExecutionConstants.XeroCorrelationId, xeroCorrelationId }
                }
            };
            return await httpClient.SendAsync(request, cancellationToken);
        });

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to get payment request for paymentRequestId: {PaymentRequestId}. Response: {ResponseBody}",
                paymentRequestId, responseBody);
            var paymentExecutionError = new PaymentExecutionError("Failed to get payment request.", response.StatusCode);
            return Result.Fail(paymentExecutionError);
        }
        var paymentRequestContent = await response.Content.ReadAsStringAsync();
        var paymentRequest = TryDeserializeResponseContent(paymentRequestContent, paymentRequestId);

        return paymentRequest;
    }

    public async Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetAsync("ping", cancellationToken);
    }

    private PaymentRequest TryDeserializeResponseContent(string responseStringContent, Guid paymentRequestId)
    {
        var deserializedResponse =
            JsonSerializer.Deserialize<PaymentRequest>(responseStringContent, _deserializerOptions) ?? throw new InvalidOperationException($"Failed to deserialize response content for Payment Request Id: {paymentRequestId}");
        return deserializedResponse;
    }
}
