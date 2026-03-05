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
using Xero.Accelerators.Api.Core.Conventions.Versioning;

namespace Xero.Accelerators.Api.Core.Observability.Logging;

public class GitHashEnricher : ILogEventEnricher
{
    private const string GitHashKey = "GitHash";
    private static readonly string _gitHashValue = ComponentVersion.FromAssembly().GitHash;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(GitHashKey, _gitHashValue));
    }
}
