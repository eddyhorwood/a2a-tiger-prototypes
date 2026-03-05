// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;

namespace Xero.Accelerators.Api.UnitTests.OpenApi;

public class OpenApiTestsDocBuilder
{
    private readonly List<RouteEndpoint> _routeEndpoints = new();

    public OpenApiDocument Document { get; }

    public IEnumerable<EndpointDataSource> Endpoints
        => new EndpointDataSource[] { new DefaultEndpointDataSource(_routeEndpoints) };

    public OpenApiTestsDocBuilder()
    {
        Document = new OpenApiDocument { Paths = new OpenApiPaths(), Components = new OpenApiComponents() };
    }

    internal OpenApiOperation AddEndpoint(params object[] metadata)
    {
        var endpointName = $"endpoint{_routeEndpoints.Count + 1}";
        var endpoint = OpenApiTestsHelpers.CreateRouteEndpoint(endpointName, metadata);

        _routeEndpoints.Add(endpoint);

        var op = new OpenApiOperation { OperationId = endpointName };
        Document.Paths.Add($"/{endpointName}", new OpenApiPathItem { Operations = { [OperationType.Get] = op } });

        return op;
    }
}
