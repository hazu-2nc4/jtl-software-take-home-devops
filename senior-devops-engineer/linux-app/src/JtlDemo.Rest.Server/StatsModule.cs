using JtlDemo.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JtlDemo.Rest.Server;

/// <summary>Portable statistics module moved out of the Windows assembly.</summary>
public sealed class StatsModule : IApiModule
{
    public string Name => "Stats";

    public static int Value => 42;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/stats", () => Results.Ok(new { value = Value }));
    }
}
