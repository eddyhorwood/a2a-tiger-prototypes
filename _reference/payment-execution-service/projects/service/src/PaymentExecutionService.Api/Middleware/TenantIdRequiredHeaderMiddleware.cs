using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

namespace PaymentExecutionService.Middleware;

public class TenantIdRequiredHeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var attribute = endpoint?.Metadata.GetMetadata<RequiresTenantIdHeaderAttribute>();

        if (attribute != null)
        {
            if (!context.Request.Headers.TryGetValue(Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId,
                    out var xeroTenantId))
            {
                await context.WriteProblemDetailsAsync(new ProblemDetails
                {
                    Title = "Xero-Tenant-Id not found",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The Xero-Tenant-Id header is required."
                });
                return;
            }

            if (!Guid.TryParse(xeroTenantId, out var xeroTenantIdGuid))
            {
                await context.WriteProblemDetailsAsync(new ProblemDetails
                {
                    Title = "Xero-Tenant-Id is empty",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The Xero-Tenant-Id header must be a UUID."
                });
                return;
            }

            if (xeroTenantIdGuid == Guid.Empty)
            {
                await context.WriteProblemDetailsAsync(new ProblemDetails
                {
                    Title = "Xero-Tenant-Id is empty",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The Xero-Tenant-Id header should not be an empty GUID."
                });
                return;
            }
        }

        await next(context);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresTenantIdHeaderAttribute : Attribute
{
    public RequiresTenantIdHeaderAttribute()
    {
    }
}
