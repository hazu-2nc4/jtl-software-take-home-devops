# Windows-to-Linux migration audit

## Scope and confidence

This is a static source and resolved-package audit of `senior-devops-engineer/app`.
It covers all handwritten application and test C# files, project files, and the
solution; generated `bin/`/`obj/` output was not treated as source. The graph
traces each route to the final public .NET objects invoked by application code.
The baseline solution built cleanly and its two composition tests passed on the
Windows SDK available for this audit.

There are seven routes. Five (`/healthz`, `/api/_config`, `/api/items`,
`/api/customers`, and `/api/stats`) should remain in a Linux image. Two
(`documents` and `printers`) should remain in a Windows supplement.

## Endpoint impact

| Route | Handler path to terminal application/framework object | Direct Windows use | Linux decision |
| --- | --- | --- | --- |
| `GET /healthz` | `Program` -> `MapGet` -> `Results.Ok({ status })` | None | Keep; presently unreachable because host startup fails first. |
| `GET /api/_config` | `Program` -> registry-derived `connectionString.Length` -> `Results.Ok` | Registry input | Keep after portable configuration replacement. |
| `GET /api/items` | catalog -> `ItemsModule` -> `IApiModule.MapEndpoints` -> `Items` -> `Item` -> `Results.Ok` | None | Keep. |
| `GET /api/customers` | catalog -> `CustomersModule` -> `IApiModule.MapEndpoints` -> `Customers` -> `Customer` -> `Results.Ok` | None | Keep. |
| `GET /api/stats` | catalog -> `StatsModule` -> `IApiModule.MapEndpoints` -> `Value` -> `Results.Ok` | None | Keep; relocate from the Windows assembly. |
| `GET /api/documents/{id}/preview` | catalog -> `DocumentExportModule` -> `Bitmap` -> `Graphics` -> `SystemFonts` -> `Bitmap.Save(ImageFormat.Png)` -> `Results.File` | GDI+ / `System.Drawing.Common` | Exclude; retain as a Windows capability. |
| `GET /api/printers` | catalog -> `PrinterModule` -> `PrinterSettings.InstalledPrinters` -> `Cast` -> `ToArray` -> `Results.Ok` | Installed printer discovery | Exclude; retain as a Windows capability. |

## Findings and blast radius

| ID | Windows dependency traced to its final source-visible object | Trigger | Blast radius | Migration treatment |
| --- | --- | --- | --- | --- |
| WIN-001 | `Registry.LocalMachine.OpenSubKey` -> `RegistryKey.GetValue` under `HKLM\\SOFTWARE\\JTL\\Wawi` | Every process start | **Critical:** throws before port binding; all seven routes, health checks, and readiness fail. | Replace with `IConfiguration` (environment variable/mounted config); validate configuration at startup without a Windows store. |
| WIN-002 | `EventLog.WriteEntry` with `EventLogEntryType.Information` | Every process start | **Critical:** executes before port binding; all seven routes are unavailable if unsupported. | Use structured `ILogger` output to stdout/stderr for container log collection. |
| WIN-003 | `ApiModuleCatalog` directly constructs the Windows assembly types; `Rest.Server` then has a Windows-only project reference and TFM | Build/composition | **High:** makes a shared library and host `net8.0-windows`, dragging the two excluded features and `System.Drawing.Common` into every deployment. It blocks the Linux image, including otherwise portable routes. | Make the portable catalog contain Items, Customers, and moved Stats. Add Windows-only composition separately. |
| WIN-004 | `Bitmap` -> `Graphics.FromImage` -> `DrawString(SystemFonts.DefaultFont)` -> `Bitmap.Save(ImageFormat.Png)` (`System.Drawing.Common` 8.0.10) | Documents request | **Feature-specific after WIN-003 is cut:** document preview disappears from the Linux product surface. | Exclude from Linux; front it with a Windows service/worker or replace with supported cross-platform rendering. |
| WIN-005 | `PrinterSettings.InstalledPrinters` (`System.Drawing.Printing`) | Printers request | **Feature-specific after WIN-003 is cut:** printer discovery cannot meanfully inspect a container host. | Exclude from Linux; use a Windows print service or explicitly configured remote printers. |

`System.Drawing.Common` also restores `Microsoft.Win32.SystemEvents` transitively.
The host's resolved graph additionally brings `System.Security.AccessControl` and
`System.Security.Principal.Windows` through its registry/Event Log packages. They
are consequences of the direct dependencies, not extra source call sites.

## Non-findings that matter

- `StatsModule` is portable despite residing in `JtlDemo.Modules.Windows`; moving it
  is a low-risk relocation with no behavior change expected (`Value` is `42`).
- `ItemsModule` and `CustomersModule` contain no Windows API call.
- No P/Invoke, `DllImport`, COM interop, native-DLL invocation, drive-root path,
  or other Windows namespace was found outside the dependencies above.

## Suggested cut order

1. Replace the registry and Event Log startup operations; this restores process
   startup and makes `/healthz` a viable container probe.
2. Split composition at `ApiModuleCatalog`, move `StatsModule` into the portable
   slice, and retarget the Linux host/shared projects to `net8.0`.
3. Keep Documents and Printers compiled/registered only by a Windows supplement;
   preserve their endpoints there so the existing Windows installation still works.
4. Add Linux-host integration checks for the five kept routes and explicit
   contract/availability handling for the two excluded routes.
