// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System;
using System.Net.Http;

namespace Xero.Accelerators.Api.ComponentTests.Observability.Correlation;
public static class CorrelationExtensions
{

    public static HttpClient WithXeroCorrelationId(this HttpClient client, string? correlationId = "")
    {
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        client.DefaultRequestHeaders.Add(Core.Constants.HttpHeaders.XeroCorrelationId, correlationId);
        return client;
    }
}
