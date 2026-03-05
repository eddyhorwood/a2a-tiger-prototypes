// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

namespace Xero.Accelerators.Api.Core.Conventions.Cataloguing;

public record CatalogueMetadata
(
    string Name,
    string Description,
    XeroApiType ApiType,
    string ComponentUuid,
    Dictionary<string, string> EnvironmentUrls
);

public enum XeroApiType
{
    Experience,
    Product,
    Platform,
    Internal
}
