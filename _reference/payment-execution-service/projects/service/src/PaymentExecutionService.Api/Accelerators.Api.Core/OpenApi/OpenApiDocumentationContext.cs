// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics;

namespace Xero.Accelerators.Api.Core.OpenApi;

public interface IOpenApiDocumentationContext
{
    void Warn(string message, params object[] args);
}

public class OpenApiDocumentationContext(ILogger<OpenApiDocumentationContext> logger) : IOpenApiDocumentationContext
{
    // NOTE: The GenerateOpenApiSpec MSBuild target looks for logs following this template.
    // If you update this, ensure you also update 'OpenApiWarningPrefix' in the
    // Xero.Accelerators.Api.Core.OpenApi.targets file.
    private const string LogTemplate = "OpenAPI warning: {Message}";

    public void Warn(string message, params object[] args)
    {
        // record the calling type's name, to help identify where a warning originated from
        var callerStackFrame = new StackTrace().GetFrame(1);
        var callerMethodInfo = callerStackFrame?.GetMethod();
        var callerTypeName = callerMethodInfo?.ReflectedType?.Name;

        // log the warning close to the source, to help identify where a warning originated from
        var logTags = new Dictionary<string, object> { ["WarningSource"] = callerTypeName ?? "<unknown>" };
        using var _ = logger.BeginScope(logTags);
        var renderedMessage = string.Format(message, args);
        logger.LogWarning(LogTemplate, renderedMessage);
    }
}
