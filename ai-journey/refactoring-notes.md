# Linux replatform refactoring notes

## Purpose and boundary

This refactor creates a deployable Linux API slice without changing the legacy implementation. The original `senior-devops-engineer/app/` is frozen as the comparison point. It still contains the complete, Windows-coupled application and is not referenced by either new solution. `linux-app/` and `windows-app/` are the maintained implementations.

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

`shared/JtlDemo.Abstractions` contains the common ASP.NET endpoint-registration interface. It is portable across OS targets, not a queue-message or domain contract. The two catalogs own their explicit module lists, so neither slice accidentally imports the other:

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

Missing, empty, or whitespace configuration stops startup with this actionable error:

```
Missing required configuration value ConnectionStrings__JtlDemo.
Set it as an environment variable or configuration provider value.
```

`GET /api/_config` deliberately returns only `{ "configured": true }` after successful startup; it does not return the connection string. This demo uses in-memory data, so configuration injection does not establish database connectivity.

## Linux container

`linux-app/Dockerfile` is a four-stage Docker build:

1. `restore`: uses `mcr.microsoft.com/dotnet/sdk:8.0.424-azurelinux3.0-amd64` and restores only Linux/shared projects.
2. `test`: runs the Linux composition tests inside the image build.
3. `publish`: emits framework-dependent host and probe outputs.
4. `final`: uses `mcr.microsoft.com/dotnet/aspnet:8.0.30-azurelinux3.0-distroless-extra-amd64`, copies only published files, exposes port 8080, and runs as the image-provided non-root account (`$APP_UID`, observed as UID 1654).

Both `FROM` instructions also pin the resolved manifest digest. Review the SDK/runtime digests monthly and when security advisories arrive, then rebuild and verify before promotion. Fresh audited restores run independently of Docker layer caching.

The repository `.dockerignore` omits the legacy `app/`, `windows-app/`, build outputs, Git metadata, and analysis cache from the container context. The image build therefore cannot embed the Windows slice by mistake.

Build for the target host architecture:

```powershell
Set-Location senior-devops-engineer
$imageTag = 'local-' + [guid]::NewGuid().ToString('N')
docker buildx build --platform linux/amd64 --load --tag "jtldemo-linux:$imageTag" --file linux-app/Dockerfile .
docker run --rm --publish 127.0.0.1:8080:8080 --env ConnectionStrings__JtlDemo='Server=example' "jtldemo-linux:$imageTag"
```

## Health check contract

The image declares the following Docker health check:

```
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD ["dotnet", "/healthcheck/JtlDemo.Container.Healthcheck.dll"]
```

Docker runs that command *inside the container*, not from the host and not from another container. The executable makes a two-second HTTP request to `http://127.0.0.1:8080/healthz`.

The app maps `/healthz` to HTTP 200 with `{ "status": "ok" }`. A successful response makes the executable exit `0`; a network failure, timeout, or non-success status makes it exit `1`. Docker initially marks the container `starting`; the ten-second start period gives initialization grace for failures, but a successful probe can mark it healthy during that period. After initialization, probes run at the 30-second interval. Three counted consecutive failures mark it `unhealthy`; a successful probe changes it back to `healthy`. Poll with a deadline instead of assuming a fixed ten-second wait is sufficient. [Docker health-check semantics](https://docs.docker.com/reference/dockerfile/#healthcheck).

This health status is observability metadata. Docker does not restart a container merely because it is unhealthy. A deployment platform must act on it explicitly. In Kubernetes, define native `startupProbe`, `readinessProbe`, and `livenessProbe` HTTP checks instead; Kubernetes does not rely on the Dockerfile `HEALTHCHECK` instruction for pod lifecycle.

The chart already defines all three native probes. Using the same HTTP check is sufficient for these in-memory routes; add dependency-aware readiness when real dependencies appear, while keeping liveness independent of external availability.

## Manual API validation

### Linux: Docker only

The Linux check requires Docker Desktop, but no host .NET installation. From `senior-devops-engineer/`, build and start the image:

```powershell
$imageTag = 'local-' + [guid]::NewGuid().ToString('N')
docker buildx build --platform linux/amd64 --load --tag "jtldemo-linux:$imageTag" --file linux-app/Dockerfile .
docker run --detach --name jtldemo-linux-check --publish 127.0.0.1:8080:8080 --env ConnectionStrings__JtlDemo='Server=validation' "jtldemo-linux:$imageTag"
```

Wait for Docker to report `healthy`, with a bounded deadline:

```powershell
$deadline = (Get-Date).AddSeconds(90)
do {
    $health = docker inspect --format '{{.State.Health.Status}}' jtldemo-linux-check
    if ($health -eq 'healthy') { break }
    if ($health -eq 'unhealthy') { throw 'Container unhealthy; inspect docker logs' }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)
if ($health -ne 'healthy') { throw 'Health deadline exceeded; inspect docker logs' }
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

## Historical baseline verification

The table below records the pre-review baseline from 2026-09-06/07. The original image digest was not recorded; its size is historical, not a current-build guarantee. See the implementation results in [refactoring-review.md](refactoring-review.md) for the correction pass.

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
- Prove a supported unattended printing adapter and execution model before packaging a worker. The current Printers endpoint only lists printers; job submission and acknowledgement-loss recovery require new behavior.
- Evaluate renderer fidelity, fonts, maintenance, and performance. A suitable portable renderer can initially run inside the Linux host; separate hosting needs an isolation, scaling, or ownership justification.
- Validate local Kind deployment/rollback and provision the selected Azure platform later. Native probes and the application chart are already present.

## Planned Azure deployment model (not implemented)

The local Helm chart expresses the Linux API deployment contract; lint/render checks do not establish a working cluster deployment. The intended Azure model remains subject to compatibility and workflow decisions:

| Capability | Planned hosting | Boundary still to define |
| --- | --- | --- |
| Items, Customers, Stats | AKS Deployment on a Linux node pool | Azure Container Registry delivery, ingress, autoscaling, workload identity, Key Vault-backed configuration, monitoring, and per-environment namespaces/values. |
| Documents | An isolated Azure App Service/API candidate, if the current Windows/GDI rendering engine is proven compatible with the selected App Service Windows hosting model | Validate Windows-container support, GDI behavior, identity/network requirements, and whether a cross-platform renderer makes this PaaS split unnecessary. |
| Printers | Dedicated Windows print worker that owns installed-printer access and serves the Printers capability | Select the Windows compute location, queue/job contract, printer-network access, retry/idempotency policy, and client-facing status/download workflow. |

The printing direction does not assume the existing API is a supported unattended adapter: Microsoft documents `System.Drawing.Printing` as unsupported in Windows services and ASP.NET applications/services. Retaining it preserves the exercise's implementation; production needs a supported adapter, suitable identity, and access to the actual printers/drivers. Moving it into a Windows container alone does not establish support. [Microsoft printing guidance](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.printing?view=windowsdesktop-8.0). Before client cutover, define route ownership and authentication across both hosts; the Windows supplement is not the original complete API.

No Azure resource, credential, or production connection string is created by this repository. The `values-local-dev.yaml` `Server=local-dev` value is a Kind-only mock. `values-production.yaml` is a non-runnable skeleton that must receive its placeholder values from a protected delivery system.

For production, retain the application's `ConnectionStrings__JtlDemo` environment-variable contract but change its Helm source. Azure Key Vault should hold each environment value; workload identity should grant the target AKS service account only the ability to retrieve its own secret; and the Secrets Store CSI Driver should materialize a namespaced Kubernetes Secret. The Deployment injects it with `env.valueFrom.secretKeyRef` and mounts the CSI volume to trigger the synchronization.

The production skeleton uses `config.source: existingSecret` with `config.existingSecret.name` and `config.existingSecret.key`. Its schema and guards require a valid image digest, input types/formats, no remaining placeholders, and the Azure preset's Key Vault, Ingress, HPA, and secret-reference contract. Both modes select Linux AMD64 nodes; HPA owns replica count when enabled. These validations do not create or verify Azure resources. Before enabling production, provide the ingress controller and Azure Load Balancer outside the chart, configure TLS/certificates, apply NetworkPolicies and PodDisruptionBudget, and validate scaling and recovery against real load.

Secret contents and credentials stay out of Git/Helm values; non-secret image digests, hostnames, and resource IDs can be versioned under the protected GitOps policy. The platform owner enables CSI rotation; the application delivery owner restarts Pods after synchronization and verifies rollout and endpoint access before retiring the previous credential. Existing environment variables do not refresh automatically. The [chart README](../senior-devops-engineer/helm/jtldemo-linux/README.md) records the initial manual restart procedure; validate it in QAT before adding automation.
