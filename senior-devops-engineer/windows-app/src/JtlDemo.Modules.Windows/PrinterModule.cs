using System.Drawing.Printing;
using JtlDemo.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JtlDemo.Modules.Windows;

/// <summary>Windows-only installed-printer discovery.</summary>
public sealed class PrinterModule : IApiModule
{
    public string Name => "Printers";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/printers", () =>
        {
            var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
            return Results.Ok(printers);
        });
    }
}
