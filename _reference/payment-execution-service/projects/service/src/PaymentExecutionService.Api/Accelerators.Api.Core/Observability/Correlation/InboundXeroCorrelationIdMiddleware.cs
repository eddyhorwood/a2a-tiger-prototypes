// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Serilog.Context;
using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Observability.Monitoring;

namespace Xero.Accelerators.Api.Core.Observability.Correlation;

public class InboundXeroCorrelationIdMiddleware(RequestDelegate next, ProblemDetailsFactory problemDetailsFactory, IMonitoringService monitoringService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var attribute = endpoint?.Metadata.GetMetadata<AllowNoXeroCorrelationIdAttribute>();

        var xeroCorrelationIdRequired = attribute == null;
        var hasXeroCorrelationId = context.Request.Headers.TryGetValue(Constants.HttpHeaders.XeroCorrelationId, out var xeroCorrelationId);

        // If a Xero-Correlation-Id header was not included in the incoming request, and not required for the endpoint, generate a new one.
        // Satisfies XREQ-170
        if (!xeroCorrelationIdRequired)
        {
            if (!hasXeroCorrelationId)
            {
                xeroCorrelationId = Guid.NewGuid().ToString();
                context.Request.Headers[Constants.HttpHeaders.XeroCorrelationId] = xeroCorrelationId;
            }
            await next(context);
            return;

        }

        if (!hasXeroCorrelationId || string.IsNullOrEmpty(xeroCorrelationId))
        {
            await context.WriteProblemDetailsAsync(problemDetailsFactory.CreateCommonProblem(context, "correlation-id-not-found", StatusCodes.Status400BadRequest, "The Xero-Correlation-Id header is required."));
            return;
        }

        if (!Guid.TryParse(xeroCorrelationId, out var xeroCorrelationIdGuid))
        {
            await context.WriteProblemDetailsAsync(problemDetailsFactory.CreateCommonProblem(context, "correlation-id-not-valid", StatusCodes.Status400BadRequest, "The Xero-Correlation-Id header should be a valid GUID."));
            return;
        }

        if (xeroCorrelationIdGuid == Guid.Empty)
        {
            await context.WriteProblemDetailsAsync(problemDetailsFactory.CreateCommonProblem(context, "correlation-id-is-empty", StatusCodes.Status400BadRequest, "The Xero-Correlation-Id header should not be an empty GUID."));
            return;
        }

        // Satisfies XREQ-171
        using (LogContext.PushProperty(Constants.HttpHeaders.XeroCorrelationId, xeroCorrelationId))
        {
            monitoringService.AddTransactionAttribute(Constants.HttpHeaders.XeroCorrelationId, xeroCorrelationId.FirstOrDefault() ?? string.Empty);
            await next.Invoke(context);
        }
    }
}
