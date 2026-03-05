// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Serilog.Core;
using Serilog.Events;

namespace Xero.Accelerators.Api.Core.Observability.Logging;

public class LogLevelEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var level = logEvent.Level.ToString();
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Level", level));
    }
}
