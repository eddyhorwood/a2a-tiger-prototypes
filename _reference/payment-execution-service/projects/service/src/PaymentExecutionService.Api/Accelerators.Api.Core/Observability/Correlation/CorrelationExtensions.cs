// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Xero.Accelerators.Api.Core.Observability.Monitoring;
using Xero.Accelerators.Api.Core.OpenApi;

namespace Xero.Accelerators.Api.Core.Observability.Correlation;

public static class CorrelationExtensions
{
    private static readonly AllowNoXeroCorrelationIdAttribute _allowNoXeroCorrelationIdMetadata = new();

    public static TBuilder AllowNoXeroCorrelationId<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(_allowNoXeroCorrelationIdMetadata);
        });
        return builder;
    }

    public static IApplicationBuilder UseInboundXeroCorrelationIdMiddleware(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<InboundXeroCorrelationIdMiddleware>(new NewRelicMonitoringService())
            .UseOpenApiDocumentFilter<CorrelationIdDocumentFilter>(order: 200);
    }
}
