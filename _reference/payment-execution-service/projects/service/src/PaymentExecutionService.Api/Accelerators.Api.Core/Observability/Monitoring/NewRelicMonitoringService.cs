// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

namespace Xero.Accelerators.Api.Core.Observability.Monitoring;

public class NewRelicMonitoringService : IMonitoringService
{
    public string GetTraceId()
    {
        var agent = NewRelic.Api.Agent.NewRelic.GetAgent();
        return agent.TraceMetadata.TraceId;
    }

    public void ReportError(Exception exception, IDictionary<string, string>? attributes)
    {
        NewRelic.Api.Agent.NewRelic.NoticeError(exception, attributes);
    }

    public void AddTransactionAttribute(string key, object value)
    {
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute(key, value);
    }
}
