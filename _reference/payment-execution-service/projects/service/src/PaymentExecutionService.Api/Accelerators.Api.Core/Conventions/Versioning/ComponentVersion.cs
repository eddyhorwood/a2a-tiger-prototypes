// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Reflection;

namespace Xero.Accelerators.Api.Core.Conventions.Versioning;

public readonly record struct ComponentVersion
{
    internal string GitHash { get; private init; }
    internal string Version { get; private init; }

    public static ComponentVersion FromAssembly()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        return new ComponentVersion
        {
            GitHash = executingAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(ama => ama.Key == "Xero.Accelerators.Api.Core.Conventions.Versioning.GitHash").Value!,
            Version = executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version
        };
    }
}
