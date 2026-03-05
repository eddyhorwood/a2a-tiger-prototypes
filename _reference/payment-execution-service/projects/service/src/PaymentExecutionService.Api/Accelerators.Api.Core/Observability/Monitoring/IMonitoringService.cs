// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

namespace Xero.Accelerators.Api.Core.Observability.Monitoring;

public interface IMonitoringService
{
    string GetTraceId();
    void ReportError(Exception exception, IDictionary<string, string>? attributes);
    void AddTransactionAttribute(string key, object value);
}
