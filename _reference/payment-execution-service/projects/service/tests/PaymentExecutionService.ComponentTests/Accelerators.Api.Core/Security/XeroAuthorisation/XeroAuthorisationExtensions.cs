// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Net.Http;
using Xero.Accelerators.Api.Core;

namespace Xero.Accelerators.Api.ComponentTests.Security.XeroAuthorisation;

public static class XeroAuthorisationExtensions
{
    public static HttpClient WithXeroUserId(this HttpClient client, string userId)
    {
        client.DefaultRequestHeaders.Add(Constants.HttpHeaders.XeroUserId, userId);
        return client;
    }
}
