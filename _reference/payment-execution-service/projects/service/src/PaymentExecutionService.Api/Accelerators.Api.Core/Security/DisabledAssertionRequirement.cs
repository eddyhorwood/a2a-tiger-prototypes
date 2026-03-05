// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Authorization;

namespace Xero.Accelerators.Api.Core.Security;

public class DisabledAssertionRequirement : IAuthorizationRequirement
{
}

public class DisabledAuthorizationHandler : AuthorizationHandler<DisabledAssertionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DisabledAssertionRequirement requirement)
    {
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
