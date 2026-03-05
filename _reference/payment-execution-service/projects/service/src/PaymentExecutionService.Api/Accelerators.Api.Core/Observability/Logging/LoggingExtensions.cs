// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Serilog;
using Serilog.Configuration;
using Xero.Accelerators.Api.Core.Observability.Monitoring;

namespace Xero.Accelerators.Api.Core.Observability.Logging;

public static class LoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>(new NewRelicMonitoringService());
    }

    // This is referenced in appsettings.json to ensure all logs contain the log level
    // The CompactJsonFormatter excludes log level for Information logs
    public static LoggerConfiguration WithLogLevel(
        this LoggerEnrichmentConfiguration enrich)
    {
        ArgumentNullException.ThrowIfNull(enrich);

        return enrich.With<LogLevelEnricher>();
    }

    // This is referenced in appsettings.json to ensure all logs contain the Git hash.
    public static LoggerConfiguration WithGitHash(
        this LoggerEnrichmentConfiguration enrich)
    {
        ArgumentNullException.ThrowIfNull(enrich);

        return enrich.With<GitHashEnricher>();
    }
}
