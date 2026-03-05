// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Mvc.Routing;

namespace Xero.Accelerators.Api.Core.Conventions.Routing;

[AttributeUsage(AttributeTargets.Class)]
public class XeroTenantRouteAttribute(string route, XeroTenant tenant) : Attribute, IRouteTemplateProvider
{
    private readonly string _tenant = tenant switch
    {
        XeroTenant.Organisation => "organisations",
        XeroTenant.Practice => "practices",
        XeroTenant.Ecosystem => "ecosystem",
        XeroTenant.Client => "clients",
        _ => throw new ArgumentOutOfRangeException(nameof(tenant), "Invalid Tenant Type")
    };

    public string Template => $"{route}/{_tenant}/{{xeroTenantId}}";
    public int? Order => 0;
    public string Name => route;
}

public enum XeroTenant
{
    Organisation,
    Practice,
    Ecosystem,
    Client
}
