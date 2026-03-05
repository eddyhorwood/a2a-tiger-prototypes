using System.Globalization;
using System.Net;
using PaymentExecution.Common;
using Polly.CircuitBreaker;
using Polly.Registry;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

namespace PaymentExecutionService.Middleware.ActionCircuitBreakers;

public class CircuitMiddleware(
    RequestDelegate next,
    ILogger<CircuitMiddleware> logger,
    ResiliencePipelineProvider<string> pipelineProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var uniqueEndpointKey = GetUniqueEndpointKey(context);
        if (uniqueEndpointKey == null || !pipelineProvider.TryGetPipeline(uniqueEndpointKey, out var pipeline))
        {
            // Fallback to default behavior if no pipeline is found
            await next(context);
            return;
        }

        logger.LogInformation("Executing circuit breaker pipeline for endpoint: {EndpointKey}", uniqueEndpointKey);
        try
        {
            await pipeline
                .ExecuteAsync(async _ => await next(context));
        }
        catch (BrokenCircuitException ex)
        {
            await GenerateCircuitBreakerOpenErrorResponse(context, ex.RetryAfter);
        }
    }

    private static string? GetUniqueEndpointKey(HttpContext context)
    {
        var isActionFound = context.Request.RouteValues.TryGetValue("action", out var action);
        var isControllerFound = context.Request.RouteValues.TryGetValue("controller", out var controller);
        var version = context.GetRequestedApiVersion();

        if (!isActionFound || !isControllerFound || version == null)
        {
            return null;
        }

        return $"v{version.MajorVersion}.{version.MinorVersion ?? 0}:{controller}Controller:{action}";
    }

    private static async Task GenerateCircuitBreakerOpenErrorResponse(HttpContext context, TimeSpan? retryAfter)
    {
        var retrySeconds = retryAfter?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        var problemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Circuit open error",
            Detail = $"Retry available in {retrySeconds} seconds.",
            Type = "https://common.service.xero.com/schema/problems/circuit-breaker-open",
            ErrorCode = ErrorConstants.ErrorCode.GenericExecutionError,
            ProviderErrorCode = null
        };
        problemDetails.Extensions.Add("retryAfter", retrySeconds);
        await context.WriteProblemDetailsAsync(problemDetails);
    }
}
