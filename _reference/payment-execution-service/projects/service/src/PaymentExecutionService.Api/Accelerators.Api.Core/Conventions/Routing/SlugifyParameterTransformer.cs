// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Xero.Accelerators.Api.Core.Conventions.Routing;

public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object? value)
    {
        var route = value?.ToString();

        if (string.IsNullOrWhiteSpace(route))
        {
            Debug.Assert(false, $"Empty route parameter was passed to {nameof(SlugifyParameterTransformer)}.");
            return null;
        }

        var transformedRoute = Regex.Replace(route,
            "([A-Z])([a-z])",
            "-$1$2",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        transformedRoute = Regex.Replace(transformedRoute,
            "([a-z])([A-Z])",
            "$1-$2",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)).ToLowerInvariant();

        if (transformedRoute[0] == '-')
        {
            transformedRoute = transformedRoute.Remove(0, 1);
        }

        return transformedRoute;
    }
}

public class SlugifyParameterTransformerConvention : RouteTokenTransformerConvention
{
    public SlugifyParameterTransformerConvention() : base(new SlugifyParameterTransformer()) { }
}
