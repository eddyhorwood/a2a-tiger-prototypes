// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Xero.Accelerators.Api.Core.Conventions.ErrorHandling;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using Xero.Authorisation.Integration.Common;
using Xero.Authorisation.Integration.NetCore.Sdk;

namespace Xero.Accelerators.Api.Core.Security.XeroAuthorisation;

public class ContextProvider(IHttpContextAccessor contextAccessor) : ContextProviderBase(contextAccessor)
{

    // GetCorrelationId is setup by default, it attempts to fetch the Id from the Xero-Correlation-Id header.
    // GetSessionId is setup by default, it attempts to fetch the Id from the auth token sid as long as the token is a user claims token.

    public override UserId GetUserId(HttpContext context)
    {
        if (context.User == null)
        {
            throw new UnauthorizedAccessException($"User token not present in {nameof(context)}");
        }

        var xeroUserIdFeature = context.Features.Get<XeroUserIdFeature>() ?? throw ErrorHandlingExtensions.CreateBadHttpRequestException("invalid-xero-user-id",
                "A Xero User ID value was not specified.", "A Xero User ID value was not specified.");
        return new UserId(xeroUserIdFeature.XeroUserId);
    }

    public override TenantId GetTenantId(HttpContext context)
    {
        var tenantId = context.GetRouteData().Values["xeroTenantId"]!.ToString();
        if (!Guid.TryParse(tenantId, out var guid))
        {
            throw ErrorHandlingExtensions.CreateBadHttpRequestException("invalid-xero-tenant-id",
                "The Xero Tenant Id URL parameter value was not a valid GUID.",
                "The Xero Tenant Id URL parameter value was not a valid GUID.");
        }

        return new TenantId(guid);
    }

    // In some instances a resourceId is required for being compliant with auditing. see the Authorisation docs for additional details - https://authz.xero.dev/details/audit?_highlight=resour#including-the-resourceid-in-the-authz-request
    public override string? GetResourceId(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var endpoint = context.GetEndpoint() as RouteEndpoint;
        var resourceIdAuthorisationAttribute = endpoint?.Metadata.GetMetadata<ResourceIdAuthorizeAttribute>();
        if (resourceIdAuthorisationAttribute != null)
        {
            var customValue = resourceIdAuthorisationAttribute.ResourceIdPathString;
            return context.Request.RouteValues[customValue]?.ToString() ?? string.Empty;
        }

        return "";
    }
}
