using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using PaymentExecution.Common;
using PaymentExecution.FeatureFlagClient;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

namespace PaymentExecutionService.Middleware;

public class ServiceFeatureFlagFilter(IFeatureFlagClient flagClient, ProblemDetailsFactory problemDetailsFactory) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var serviceFlagValue = flagClient.GetFeatureFlag(ExecutionConstants.FeatureFlags.PaymentExecutionServiceEnabled).Value;
        if (!serviceFlagValue)
        {
            await context.HttpContext.WriteProblemDetailsAsync(problemDetailsFactory.CreateCommonProblem(context.HttpContext,
                "payment-execution-service-disabled", StatusCodes.Status503ServiceUnavailable,
                "The payment execution service is currently disabled."));
            return;
        }
        await next.Invoke();
    }
}
