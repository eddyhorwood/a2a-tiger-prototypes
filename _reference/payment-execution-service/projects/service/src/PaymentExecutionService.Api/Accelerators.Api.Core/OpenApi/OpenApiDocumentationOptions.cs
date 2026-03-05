// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Xero.Accelerators.Api.Core.OpenApi;

public class OpenApiDocumentationOptions
{
    public IEnumerable<FilterDescriptor> DocumentFilters => _documentFilters
        .OrderBy(orderedDescriptor => orderedDescriptor.Order)
        .ThenBy(orderedDescriptor => orderedDescriptor.Descriptor.Type.Name)
        .Select(orderedDescriptor => orderedDescriptor.Descriptor);

    private class OrderedFilterDescriptor //NOSONAR for sealed modifier 
    {
        public OrderedFilterDescriptor(Type filterType, int order, params object[] filterArgs)
        {
            Descriptor = new FilterDescriptor
            {
                Type = filterType,
                Arguments = filterArgs
            };
            Order = order;
        }

        public FilterDescriptor Descriptor { get; }
        public int Order { get; }
    }

    private readonly List<OrderedFilterDescriptor> _documentFilters = new();

    public void AddFilter<T>(int order, params object[] filterArgs) where T : IDocumentFilter
    {
        _documentFilters.Add(new OrderedFilterDescriptor(typeof(T), order, filterArgs));
    }
}
