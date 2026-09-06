using JtlDemo.Abstractions;

namespace JtlDemo.Modules.Windows;

/// <summary>Composition root for the capabilities intentionally retained on Windows.</summary>
public static class WindowsApiModuleCatalog
{
    public static IReadOnlyList<IApiModule> BuildApiModules() =>
    [
        new DocumentExportModule(),
        new PrinterModule(),
    ];
}
