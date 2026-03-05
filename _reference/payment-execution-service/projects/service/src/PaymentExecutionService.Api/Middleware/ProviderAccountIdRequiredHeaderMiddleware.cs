using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

namespace PaymentExecutionService.Middleware;

public class ProviderAccountIdRequiredHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var attribute = endpoint?.Metadata.GetMetadata<RequiresProviderAccountIdHeaderAttribute>();

        if (attribute != null)
        {
            if (!context.Request.Headers.TryGetValue(Constants.HttpHeaders.ProviderAccountId, out var providerAccountId))
            {
                await context.WriteProblemDetailsAsync(new ProblemDetails
                {
                    Title = "Provider-Account-Id not found",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The Provider-Account-Id header is required."
                });
                return;
            }

            if (StringValues.IsNullOrEmpty(providerAccountId))
            {
                await context.WriteProblemDetailsAsync(new ProblemDetails
                {
                    Title = "Provider-Account-Id cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Provider-Account-Id header is required."
                });
                return;
            }
        }

        await next(context);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresProviderAccountIdHeaderAttribute : Attribute
{
    public RequiresProviderAccountIdHeaderAttribute()
    {
    }
}
