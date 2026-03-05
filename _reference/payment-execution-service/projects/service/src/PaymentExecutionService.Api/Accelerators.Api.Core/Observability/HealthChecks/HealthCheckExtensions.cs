// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xero.Accelerators.Api.Core.Observability.Correlation;

namespace Xero.Accelerators.Api.Core.Observability.HealthChecks;

public record HealthCheckMetadata(IDictionary<HealthStatus, int> ResultStatusCodes);

public static class HealthCheckExtensions
{
    public static IEndpointConventionBuilder UseHealthCheckEndpoint(
        this IEndpointRouteBuilder endpoints, string endpointName, string route, HealthCheckOptions options)
    {
        return endpoints
            .MapHealthChecks(route, options)
            .WithName(endpointName)
            .WithMetadata(new HealthCheckMetadata(options.ResultStatusCodes))
            .AllowNoXeroCorrelationId();
    }

    public static IEndpointConventionBuilder UseHealthCheckEndpoint(
        this IEndpointRouteBuilder endpoints, string endpointName, string route,
        Func<HealthCheckRegistration, bool>? healthcheckPredicate = null)
    {
        var options = new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable
            }
        };
        if (healthcheckPredicate == null)
        {
            // exclude all health checks
            options.Predicate = _ => false;
            options.ResponseWriter = HealthCheckResponseWriters.WriteEmptyHealthReportAsync;
        }
        else
        {
            options.Predicate = healthcheckPredicate;
            options.ResponseWriter = HealthCheckResponseWriters.WriteHealthReportAsync;
        }

        return UseHealthCheckEndpoint(endpoints, endpointName, route, options);
    }
}
