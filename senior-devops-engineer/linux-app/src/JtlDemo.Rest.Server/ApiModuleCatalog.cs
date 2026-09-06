using JtlDemo.Abstractions;

namespace JtlDemo.Rest.Server;

/// <summary>Composition root for the API modules that can run on Linux.</summary>
public static class ApiModuleCatalog
{
    public static IReadOnlyList<IApiModule> BuildApiModules() =>
    [
        new ItemsModule(),
        new CustomersModule(),
        new StatsModule(),
    ];
}
