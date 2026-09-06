# Linux replatform refactoring notes

## Purpose and boundary

This refactor creates a deployable Linux API slice without changing the legacy implementation. The original `senior-devops-engineer/app/` remains the comparison point. It still contains the complete, Windows-coupled application and is not referenced by either new solution.

The current delivery deliberately excludes Documents and Printers from Linux. It preserves a small Windows-only supplement for those capabilities while a future worker/queue contract is designed.

## Resulting layout

```
senior-devops-engineer/
  app/                         legacy reference; unchanged
  shared/JtlDemo.Abstractions/  portable IApiModule contract
  linux-app/                    independent Linux solution
    src/JtlDemo.Rest.Server/    Items, Customers, Stats modules
    src/JtlDemo.Rest.Linux.Host/ Linux composition root
    src/JtlDemo.Container.Healthcheck/ executable Docker probe
    tests/JtlDemo.Linux.CompositionTests/
  windows-app/                  independent Windows-only solution
    src/JtlDemo.Modules.Windows/ Documents and Printers only
    src/JtlDemo.Rest.Windows.Host/
    tests/JtlDemo.Windows.CompositionTests/
```

`shared/JtlDemo.Abstractions` contains the common module interface. The two catalogs own their explicit module lists, so neither slice accidentally imports the other:

| Solution | Composition catalog | Capabilities |
| --- | --- | --- |
| `linux-app` | `ApiModuleCatalog` | Items, Customers, Stats |
| `windows-app` | `WindowsApiModuleCatalog` | Documents, Printers |

The Linux and shared source trees were statically checked for `Microsoft.Win32`, Registry, Event Log, System.Drawing, System.Printing, `net8.0-windows`, and Windows-module references; none remain. Stats was moved to the portable Linux server and has a preservation test for its value (`42`).

## Configuration and startup behavior

Both new hosts use standard `IConfiguration`; no registry bootstrap or Windows Event Log setup occurs in the Linux host. The required value is supplied through the normal connection-string provider convention:

```powershell
$env:ConnectionStrings__JtlDemo = 'Server=example'
dotnet run --project senior-devops-engineer/linux-app/src/JtlDemo.Rest.Linux.Host
```

Missing configuration stops startup with this actionable error:

```
Missing required configuration value ConnectionStrings__JtlDemo.
Set it as an environment variable or configuration provider value.
```

`GET /api/_config` deliberately returns only `{ "configured": true|false }`; it does not return the connection string.

## Linux container

`linux-app/Dockerfile` is a four-stage Docker build:

1. `restore`: uses `mcr.microsoft.com/dotnet/sdk:8.0.424-azurelinux3.0-amd64` and restores only Linux/shared projects.
2. `test`: runs the Linux composition tests inside the image build.
3. `publish`: emits framework-dependent host and probe outputs.
4. `final`: uses `mcr.microsoft.com/dotnet/aspnet:8.0.30-azurelinux3.0-distroless-extra-amd64`, copies only published files, exposes port 8080, and runs as the image-provided non-root account (`$APP_UID`, observed as UID 1654).

The repository `.dockerignore` omits the legacy `app/`, `windows-app/`, build outputs, Git metadata, and analysis cache from the container context. The image build therefore cannot embed the Windows slice by mistake.

Build for the target host architecture:

```powershell
Set-Location senior-devops-engineer
docker buildx build --platform linux/amd64 --load --tag jtldemo-linux:local --file linux-app/Dockerfile .
docker run --rm --publish 8080:8080 --env ConnectionStrings__JtlDemo='Server=example' jtldemo-linux:local
```

## Health check contract

The image declares the following Docker health check:

```
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD ["dotnet", "/healthcheck/JtlDemo.Container.Healthcheck.dll"]
```

Docker runs that command *inside the container*, not from the host and not from another container. The executable makes a two-second HTTP request to `http://127.0.0.1:8080/healthz`.

The app maps `/healthz` to HTTP 200 with `{ "status": "ok" }`. A successful response makes the executable exit `0`; a network failure, timeout, or non-success status makes it exit `1`. Docker marks the container `starting` during the ten-second start period, then runs the probe every 30 seconds. Three consecutive failed runs mark it `unhealthy`; a later successful probe changes it back to `healthy`.

This health status is observability metadata. Docker does not restart a container merely because it is unhealthy. A deployment platform must act on it explicitly. In Kubernetes, define native `startupProbe`, `readinessProbe`, and `livenessProbe` HTTP checks instead; Kubernetes does not rely on the Dockerfile `HEALTHCHECK` instruction for pod lifecycle.

## Manual API validation

### Linux: Docker only

The Linux check requires Docker Desktop, but no host .NET installation. From `senior-devops-engineer/`, build and start the image:

```powershell
docker buildx build --platform linux/amd64 --load --tag jtldemo-linux:local --file linux-app/Dockerfile .
docker run --detach --name jtldemo-linux-check --publish 8080:8080 --env ConnectionStrings__JtlDemo='Server=validation' jtldemo-linux:local
```

After at least ten seconds, Docker should show `healthy`:

```powershell
docker inspect --format '{{.State.Health.Status}}' jtldemo-linux-check
```

Then exercise the retained Linux API from PowerShell:

```powershell
$baseUrl = 'http://127.0.0.1:8080'
Invoke-RestMethod "$baseUrl/healthz"
Invoke-RestMethod "$baseUrl/api/_config"
Invoke-RestMethod "$baseUrl/api/items"
Invoke-RestMethod "$baseUrl/api/customers"
Invoke-RestMethod "$baseUrl/api/stats"
```

Expected results are a healthy status, `configured: true`, three items, two customers, and Stats value `42`. Inspect startup failures with `docker logs jtldemo-linux-check`. When finished, remove only this temporary container:

```powershell
docker rm --force jtldemo-linux-check
```

### Windows: local .NET SDK

The Windows supplement is not containerized yet. It needs a Windows host with the .NET 8 SDK installed; `dotnet --list-sdks` must show an `8.0.x` SDK. Without that prerequisite, `dotnet run` cannot start it. Set the required configuration and run the host on a deterministic local port:

```powershell
$env:ConnectionStrings__JtlDemo = 'Server=validation'
dotnet run --project senior-devops-engineer/windows-app/src/JtlDemo.Rest.Windows.Host -- --urls http://127.0.0.1:5082
```

In a second PowerShell terminal, validate the Windows-only endpoints:

```powershell
$baseUrl = 'http://127.0.0.1:5082'
Invoke-RestMethod "$baseUrl/healthz"
Invoke-RestMethod "$baseUrl/api/_config"
Invoke-WebRequest "$baseUrl/api/documents/1/preview" | Select-Object StatusCode, Headers
Invoke-RestMethod "$baseUrl/api/printers"
```

The preview call must return HTTP 200 and `Content-Type: image/png`. Printer discovery must return HTTP 200 and a JSON array; an empty array is valid when the Windows host has no installed printers. Stop the foreground host with `Ctrl+C`.

## Verified state

| Check | Result |
| --- | --- |
| Legacy solution tests | 2/2 passed |
| Linux solution tests | 2/2 passed |
| Windows supplement tests | 1/1 passed |
| Linux host smoke test | `/healthz`, `_config`, Items, Customers, Stats returned HTTP 200 |
| Missing Linux configuration | startup fails with the intended error |
| Native container build | `linux/amd64` image built and loaded |
| Container runtime | Docker health was `healthy`; all five Linux routes returned HTTP 200 |
| Final image | 72.1 MB, configured as UID 1654 |

## Deferred work

- Define the client contract and job lifecycle for Documents and Printers.
- Package the Windows-only supplement as a Windows container/print worker once that contract exists.
- Evaluate a standalone Documents rendering service if a cross-platform renderer is a better fit than the current engine.
- Add the target platform's delivery manifests and native Kubernetes probes when the hosting decision is made.

## Planned Azure deployment model (not implemented)

The local Helm chart proves only the Linux API deployment contract. The intended Azure model is deliberately split by operating-system and capability boundary:

| Capability | Planned hosting | Boundary still to define |
| --- | --- | --- |
| Items, Customers, Stats | AKS Deployment on a Linux node pool | Azure Container Registry delivery, ingress, autoscaling, workload identity, Key Vault-backed configuration, monitoring, and per-environment namespaces/values. |
| Documents | An isolated Azure App Service/API candidate, if the current Windows/GDI rendering engine is proven compatible with the selected App Service Windows hosting model | Validate Windows-container support, GDI behavior, identity/network requirements, and whether a cross-platform renderer makes this PaaS split unnecessary. |
| Printers | Dedicated Windows print worker that owns installed-printer access and serves the Printers capability | Select the Windows compute location, queue/job contract, printer-network access, retry/idempotency policy, and client-facing status/download workflow. |

No Azure resource, credential, or production connection string is created by this repository. The `values-local-dev.yaml` `Server=local-dev` value is a Kind-only mock. `values-production.yaml` is a non-runnable skeleton that must receive its placeholder values from a protected delivery system.

For production, retain the application's `ConnectionStrings__JtlDemo` environment-variable contract but change its Helm source. Azure Key Vault should hold each environment value; workload identity should grant the target AKS service account only the ability to retrieve its own secret; and the Secrets Store CSI Driver should materialize a namespaced Kubernetes Secret. The Deployment injects it with `env.valueFrom.secretKeyRef` and mounts the CSI volume to trigger the synchronization.

The production skeleton now uses `config.source: existingSecret` with `config.existingSecret.name` and `config.existingSecret.key`; it refuses to render production mode without Key Vault, Ingress, HPA, and the secret-reference contract. It does not create any Azure resource. Before enabling it, add `values.schema.json` to validate delivery inputs, provide the ingress controller and Azure Load Balancer outside the chart, configure TLS/certificates, apply NetworkPolicies and PodDisruptionBudget, validate HPA against real load, and define secret rotation, reload/restart, RBAC, and log-redaction rules. Do not template production `Secret` objects from plaintext values.

## NU1900 package-audit warning

`NU1900` does not identify a vulnerable package and is not emitted by application code. The .NET 8 SDK runs NuGet Audit during restore and queries the configured NuGet service index for advisory metadata. It reports NU1900 when that request fails.

This workspace has only the normal `https://api.nuget.org/v3/index.json` source. A restricted-shell request could not reach it, while a direct machine-level request returned HTTP 200. That indicates an execution-network boundary or transient connectivity condition, not a Linux/Windows refactoring fault and not a package-reference defect.

Keep auditing enabled. When the warning occurs on a normal developer or CI machine, first retry restore. If it persists after connectivity is restored, clear only NuGet's disposable HTTP metadata cache and restore again:

```powershell
dotnet nuget locals http-cache --clear
dotnet restore senior-devops-engineer/linux-app/JtlDemo.Linux.sln
dotnet restore senior-devops-engineer/windows-app/JtlDemo.Windows.sln
```

Do not set `NuGetAudit=false` or suppress NU1900 merely to hide a source-access failure. CI should have outbound access to NuGet (or an approved internal mirror/audit source) and should retain the audit signal.
