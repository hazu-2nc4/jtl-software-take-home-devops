# Development Plan

## Decisions

- Use a workspace-local `journaling` skill to maintain the AI journey records throughout this take-home task.
- Keep the tool and skill snapshot fixed at initialization; update the plan and prompt log after each repository-related conversation.
- Keep the initial endpoint and Windows-dependency scan as an ignored, workspace-local cache under `.cache/dependency-analysis/`; invalidate it when one of its recorded source hashes changes.
- Treat registry bootstrap and Event Log startup as whole-service Linux blockers. Treat Documents and Printers as Windows-only capabilities, while retaining Items, Customers, and Stats in the Linux slice.
- Keep the deployment topology open pending a design review: a mixed-OS AKS cluster needs a dedicated Windows node pool, while Azure Functions is not a like-for-like custom Windows-container destination for this REST service.
- Scope the current delivery to a clean Linux container for Items, Customers, and Stats. Defer the external client contract and synchronous-versus-asynchronous design for Documents and Printers.
- Do not add an AKS Windows node pool for this slice. Preserve the Windows modules and their Windows build; treat a queue-backed Windows print worker as the later production direction.
- Future follow-up: package the excluded Windows modules as a Windows container when the worker contract is defined.
- Future follow-up: evaluate extracting Documents into a standalone rendering service if a cross-platform renderer is proven preferable to the current engine.
- Retain `app/` as the untouched legacy comparison point. Build isolated `linux-app/` and `windows-app/` solutions so the Linux build has no Windows project reference; define the duplicated/shared-contract ownership deliberately to prevent drift.
- Use `IConfiguration` environment variables for the Linux connection string and fail startup with a clear replacement error when it is absent. Keep `_config` non-secret.
- Add a portable executable HTTP probe for Docker `HEALTHCHECK`; do not rely on shell utilities in the distroless image.
- Target native Linux AMD64 images and builds. Use the `amd64` tag suffix with the selected .NET SDK 8.0.424 and ASP.NET runtime 8.0.30 image versions.
- Make the container health-probe contract explicit: it always checks `http://127.0.0.1:8080/healthz`, matching the image's `ASPNETCORE_HTTP_PORTS=8080` setting; it accepts no runtime argument.
- Preserve NuGet vulnerability auditing. NU1900 is treated as a package-source connectivity/cache diagnosis, not a warning to suppress in source code.
- The Kubernetes delivery bonus has two supported Helm modes: runnable `local-dev` for Kind, and an unprovisioned Azure production skeleton. Both retain non-root settings, resource limits, and native HTTP startup/readiness/liveness probes.
- Planned Azure architecture, not yet implemented: AKS Linux node pool for Items/Customers/Stats; a separately validated App Service candidate for Documents; and a dedicated Windows print worker for Printers. The production chart skeleton contains an application Ingress, HPA, workload-identity ServiceAccount, Key Vault CSI `SecretProviderClass`, and secret-key reference; it does not provision the required Azure platform resources.
- Future GitOps direction: retain this Helm chart and have Flux deploy it through `HelmRelease`. Keep the chart/template contract version independent from the application image digest; never create a GitHub Release for every CI image. Flux image automation updates the AKS development image reference as soon as a newly eligible immutable image is available, while QAT and production retain pinned chart and image versions.

## Completed

- Initialized the AI journey documentation and journaling rule.
- Traversed the application source, project graph, resolved packages, and composition tests. Recorded a seven-endpoint dependency graph and five Windows findings, including source-level terminal API calls and migration blast radius.
- Validated the unmodified baseline: solution build passed with zero warnings/errors and 2/2 tests passed on the available Windows .NET 8 SDK. Validated that the JSON cache has seven endpoints, five Windows findings, and no source-hash mismatch.
- Created isolated `linux-app/` and `windows-app/` solutions around one shared `net8.0` abstractions project, retaining `app/` unchanged as the legacy comparison point.
- Implemented the portable Linux host, moved Stats into its portable server, replaced registry/Event Log startup with environment-backed configuration and standard logging, and added a real executable HTTP health probe for the distroless image.
- Verified the Linux solution build and 2/2 tests, Windows supplement solution build and 1/1 test, and unchanged original application tests (2/2). Verified all five Linux routes and the health-probe executable against a live host; verified missing configuration fails at startup with the intended message.
- Built and loaded the native `linux/amd64` image. Its final image is 72.1 MB, uses UID 1654, and passed its Docker health check. A running container returned HTTP 200 from health, configuration, Items, Customers, and Stats endpoints.
- Documented the full refactoring boundary, configuration, image lifecycle, health-check semantics, validations, deferred work, and NU1900 remediation in `ai-journey/refactoring-notes.md`.
- Added copy-paste API validation procedures: Docker-only validation for Linux and a .NET 8 SDK-based local validation procedure for the current Windows supplement.
- Added a parameterized Helm chart and operational runbook for local Kind deployment, API/readiness validation, and Helm rollback.
- Evolved the Helm chart into local-dev and Azure-production skeleton modes; narrowed the runbook to local-dev initialization only.
- Researched Flux `HelmRelease` and Flux `Kustomization` delivery models and documented their image-automation semantics and trade-offs in `ai-journey/flux-gitops-research.md`.
- Completed an independent refactor review in `ai-journey/refactoring-review.md`. Re-ran Linux (2/2) and Windows (1/1) composition tests; confirmed the current chart fails Helm parsing; verified the proposed helper correction only in a temporary copy. Review recommendations remain proposals, not changes to the selected architecture.

## Next Steps

1. Address the review findings before extending delivery scope: correct Helm helper nesting, use distinct image identities and a previously verified rollback revision, verify Linux and Windows HTTP behavior, reject blank required configuration, and finish the README/reflections. Helm 4.1.4 is now available and exposed the parse failure; Kind is still absent from PATH. Validate the corrected chart with the documented Helm 3 version and Kind, including a deliberate rollback exercise. These are proposed corrections from the review, not yet implemented.
2. Plan and implement the Azure deployment model only after the Documents hosting compatibility assessment and the printer worker/job contract are decided:
   - Publish immutable Linux images to Azure Container Registry and deploy Items/Customers/Stats to an AKS Linux node pool. Provision the ingress controller and its Azure Load Balancer, metrics API for HPA, workload identity/OIDC, and observability outside this application chart.
   - Store each environment connection string in Azure Key Vault. Grant only the target workload identity the Key Vault Secrets User role; enable the AKS Key Vault CSI add-on; use its `SecretProviderClass` to synchronize a namespaced Kubernetes Secret; and inject it with the chart's `env.valueFrom.secretKeyRef` contract. Apply least-privilege Kubernetes RBAC and audit access/rotation.
   - Replace the placeholders in `values-production.yaml` only in a protected delivery system. Add `values.schema.json`, image digest enforcement, ingress TLS/certificate integration, NetworkPolicies, a PodDisruptionBudget, resource/load tests for the HPA threshold, and a secret-rotation/restart strategy that never logs secret values.
   - Assess Documents on isolated App Service only after validating Windows/GDI rendering and networking/identity compatibility. Design Printers as a dedicated Windows worker with restricted printer-network access, a queue/job contract, retries, idempotency, and status reporting.
3. Later, define the gateway/contract and job lifecycle for the Windows print worker and document-rendering capability.
4. Implement Flux with `HelmRelease` as the sole owner of each workload. Configure Flux image automation to patch the Git-tracked image digest for the AKS development release immediately after a newly eligible image is available. Keep QAT and production HelmReleases version-pinned; promote the same thoroughly tested immutable image digest through QAT and then production by reviewed pull request.
