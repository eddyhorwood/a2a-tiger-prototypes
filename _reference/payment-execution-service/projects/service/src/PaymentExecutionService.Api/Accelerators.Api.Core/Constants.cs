// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

namespace Xero.Accelerators.Api.Core;

public static class Constants
{
    public static class HttpHeaders
    {
        public const string XeroCorrelationId = "Xero-Correlation-Id";
        public const string XeroUserId = "Xero-User-Id";
        public const string XeroTenantId = "Xero-Tenant-Id";
        public const string XeroClientName = "Xero-Client-Name";
    }

    public static class ClaimTypes
    {
        public const string XeroUserId = "xero_userid";
    }

    public class ExceptionDataFields
    {
        public const string ProblemDetailsIdentifier = "ProblemDetails.Identifier";
        public const string ProblemDetailsTitle = "ProblemDetails.Title";
    }
}
