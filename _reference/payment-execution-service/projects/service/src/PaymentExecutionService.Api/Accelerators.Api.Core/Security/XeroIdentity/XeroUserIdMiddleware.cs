// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.Extensions.Options;

namespace Xero.Accelerators.Api.Core.Security.XeroIdentity;

public class XeroUserIdMiddleware(RequestDelegate next, IOptionsMonitor<XeroIdentityOptions> options)
{
    private readonly XeroIdentityOptions _options = options.CurrentValue;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var xeroUserId = GetXeroUserId(context);

        if (xeroUserId.HasValue)
        {
            context.Features.Set(new XeroUserIdFeature
            {
                XeroUserId = xeroUserId.Value
            });
        }

        await next(context);
    }

    private Guid? GetXeroUserId(HttpContext context)
    {
        if (_options.RetrieveUserIdFrom.HasFlag(XeroUserIdSource.Claims))
        {
            var userClaim = context.User.Claims.SingleOrDefault(c => c.Type == Constants.ClaimTypes.XeroUserId);

            if (userClaim != null && Guid.TryParse(userClaim.Value, out var xeroUserId))
            {
                return xeroUserId;
            }
        }

        if (_options.RetrieveUserIdFrom.HasFlag(XeroUserIdSource.Header))
        {
            if (context.Request.Headers.TryGetValue(Constants.HttpHeaders.XeroUserId, out var xeroUserIdHeader) && Guid.TryParse(xeroUserIdHeader, out var xeroUserId))
            {
                return xeroUserId;
            }
        }

        return null;
    }
}

public class XeroUserIdFeature
{
    public Guid XeroUserId { get; set; }
}
