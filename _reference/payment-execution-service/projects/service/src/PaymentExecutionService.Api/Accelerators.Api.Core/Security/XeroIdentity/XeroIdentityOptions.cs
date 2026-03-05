// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

namespace Xero.Accelerators.Api.Core.Security.XeroIdentity;

public class XeroIdentityOptions
{
    public XeroUserIdSource RetrieveUserIdFrom { get; set; }
}

[Flags]
public enum XeroUserIdSource
{
    None = 0,
    Header = 1 << 0,
    Claims = 1 << 1
}
