using PaymentExecutionService.Middleware;
using Xero.Accelerators.Api.Core.OpenApi;

namespace PaymentExecutionService.Extensions;

public static class WebApplicationExtensions
{
    public static void UseEnforceProviderAccountIdAttribute(this WebApplication app)
    {
        app.UseMiddleware<ProviderAccountIdRequiredHeaderMiddleware>();
        app.UseOpenApiDocumentFilter<ProviderAccountIdHeaderDocumentFilter>(order: 199);
    }

    public static void UseEnforceTenantIdAttribute(this WebApplication app)
    {
        app.UseMiddleware<TenantIdRequiredHeaderMiddleware>();
        app.UseOpenApiDocumentFilter<TenantIdHeaderDocumentFilter>(order: 198);
    }
}
