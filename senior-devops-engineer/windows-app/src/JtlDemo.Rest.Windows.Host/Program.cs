using JtlDemo.Modules.Windows;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("JtlDemo");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing required configuration value ConnectionStrings__JtlDemo. Set it as an environment variable or configuration provider value.");
}

var app = builder.Build();

app.Logger.LogInformation("JtlDemo Windows REST host starting");

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/_config", () => Results.Ok(new { configured = connectionString.Length > 0 }));

foreach (var module in WindowsApiModuleCatalog.BuildApiModules())
{
    module.MapEndpoints(app);
}

app.Run();
