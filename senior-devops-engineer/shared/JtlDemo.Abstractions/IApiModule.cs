using Microsoft.AspNetCore.Routing;

namespace JtlDemo.Abstractions;

/// <summary>A unit of API surface that maps its routes onto a host.</summary>
public interface IApiModule
{
    string Name { get; }

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
