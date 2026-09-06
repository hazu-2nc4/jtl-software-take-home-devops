using System.Drawing;
using System.Drawing.Imaging;
using JtlDemo.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JtlDemo.Modules.Windows;

/// <summary>Windows-only document preview rendering using GDI+.</summary>
public sealed class DocumentExportModule : IApiModule
{
    public string Name => "Documents";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/documents/{id}/preview", (int id) =>
        {
            using var bitmap = new Bitmap(240, 60);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.DrawString($"Document {id}", SystemFonts.DefaultFont, Brushes.Black, 8, 20);

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return Results.File(stream.ToArray(), "image/png");
        });
    }
}
