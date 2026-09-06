# Prompt Log

## Init Working Session

**Instruction:** Create and activate a workspace-local `journaling` rule. Initialize `ai-journey/plan.md`, `ai-journey/prompts.md`, and `ai-journey/tools.md`; use the rule for all future prompts in this working directory.

**Commands / actions:** Inspected the workspace layout and existing `ai-journey` folder. Created `AGENTS.md`, the `journaling` skill, and the three requested AI journey documents. Checked the skill frontmatter and generated files directly; `git diff --check` passed. The bundled skill validator could not run because `python.exe` is unavailable in this environment.

**Outcome:** Journaling is now a repository rule. The plan records the initial decisions and next steps; this entry records the first prompt; the tool snapshot is initialized separately.

## 01 - Endpoint and Windows migration audit

**Instruction:** Scan the `senior-devops-engineer` .NET application, cache an endpoint-to-child dependency graph under `.cache`, identify every Windows dependency to the last source-visible referenced object, and assess migration blast radius.

**Commands / actions:** Read the workspace instructions and application/task documentation; traversed all handwritten C# and project files; inspected resolved direct/transitive packages with `dotnet list package --include-transitive`; ran targeted Windows API and route/composition searches; built and tested the unmodified solution. Added ignored cache artifacts: machine-readable JSON graph, Graphviz graph, and a human-readable migration audit. Validated JSON parsing and every cached source hash.

**Outcome:** The cache captures seven routes and five Windows findings. Registry configuration and Event Log startup block the whole service before it binds; the composition-root project reference blocks a Linux target; Documents and Printers are genuinely Windows-only; Stats is portable despite its current assembly. Baseline build passed with zero warnings/errors and 2/2 tests passed.

## 02 - Challenge the split-service hosting plan

**Instruction:** Stress-test a plan to put Items, Customers, and Stats in a Linux container and Documents and Printers in a Windows container, then compare a shared Kubernetes cluster with Azure App Service or Azure Functions.

**Commands / actions:** Re-read the existing dependency audit and current project plan. Reviewed current Microsoft documentation for AKS mixed-OS node pools and Windows-container lifecycle, App Service custom Windows containers and VNet limits, and Azure Functions container hosting support.

**Outcome:** Identified that a mixed AKS deployment requires a dedicated Windows node pool and deliberate scheduling, App Service can host a custom Windows image with less orchestration burden, and Azure Functions custom-container deployment is Linux-only. The service boundary, client-facing contract, and hosting choice remain user decisions before implementation.

## 03 - Confirm current slice and defer Windows-worker design

**Instruction:** Keep the discussion at analysis level. Defer the public API contract and synchronous-versus-asynchronous handling for Documents and Printers, focus the current work on a clean Linux container slice, and choose a future Windows print worker rather than adding an AKS Windows node pool.

**Commands / actions:** Recorded the user decisions in the development plan; no application source, container, or deployment resource was changed.

**Outcome:** The immediate scope is unambiguous: Linux serves Items, Customers, and Stats; the Windows modules remain buildable but are outside the Linux image. Future work will design a Windows print worker and the client-facing workflow separately.

## 04 - Review portable configuration and distroless container proposal

**Instruction:** Preserve only a verified Windows build for this iteration; record future Windows-container and possible standalone Documents-rendering work. Stress-test the proposed `IConfiguration` environment-variable migration, Stats relocation/test, and Azure Linux distroless Dockerfile, and explain the `extra`, `composite`, and `aot` .NET image variants.

**Commands / actions:** Reviewed the current .NET container-image and Azure Linux distroless documentation, including image variants and supported .NET 8 tags. Recorded the requested future follow-ups in the development plan; no product source or container file was changed pending design confirmation.

**Outcome:** Confirmed the final image must be an `aspnet` image rather than `runtime` for this ASP.NET Core app. Identified that the current `8.0` tags are not fully pinned, that `extra` is the safe globalization choice, that `composite` needs compatibility testing, that official `aot` SDK tags are .NET 10+ rather than the .NET 8 target, and that distroless needs an executable health probe because it has no shell or HTTP client.

## 05 - Select isolated application roots and ARM64 image intent

**Instruction:** Keep `app/` as the reference implementation; create isolated `linux-app/` and `windows-app/` folders, fail fast for missing environment-based configuration, add an executable health probe, and use .NET 8 Azure Linux ARM64 images.

**Commands / actions:** Inspected the current application layout and revalidated official .NET 8 Azure Linux ARM64 image metadata and Dockerfile sources. Recorded the selected layout and configuration/health-check decisions in the plan; no product source was changed while the isolation boundary is still under review.

**Outcome:** The current intent is an isolated Linux and Windows solution pair, with the original app retained for comparison. The proposed SDK/runtime patch pairing exists, but the valid tag suffix is `arm64v8`, not `arm64`. ARM64 image test stages need Buildx/QEMU or an ARM64 builder when run from an x64 workstation.

## 06 - Implement the isolated Linux and Windows slices

**Instruction:** Use a shared portable abstractions project; keep the Windows app limited to Documents and Printers; target Linux ARM64; implement the portable configuration, Stats relocation/test, distroless Dockerfile, and executable health check.

**Commands / actions:** Created `shared/`, `linux-app/`, and `windows-app/` projects and independent solutions while leaving `app/` unchanged. Implemented environment-backed configuration and standard logging in both new hosts, moved Stats into the Linux server, retained the two Windows modules in a Windows-only host, and added an ARM64 Azure Linux distroless Dockerfile plus HTTP probe. Built/tested each solution and smoke-tested the Linux endpoints and probe. Attempted Docker Buildx verification; the local Docker daemon was unavailable and permission to start Docker Desktop was declined.

**Outcome:** Linux build/test and runtime smoke verification passed; the Windows supplement builds and its test passes; original tests still pass. The only remaining verification is the requested ARM64 Docker image build/run and Docker health status, which requires an available Docker daemon.

## 07 - Fix the health-probe contract and validate native AMD64 container

**Instruction:** Resolve why the Docker health-check executable left its endpoint undecided, then correct the container platform from ARM64 to the AMD64 host platform and validate with Docker Desktop.

**Commands / actions:** Replaced the probe's former command-line endpoint with its image-level contract, `http://127.0.0.1:8080/healthz`; rebuilt and smoke-tested it against the Linux host. Built `linux-app/Dockerfile` with `docker buildx build --platform linux/amd64 --load`, inspected its platform, user, and size, then ran it with an environment-supplied connection string. Confirmed Docker health and HTTP 200 responses from `/healthz`, `/api/_config`, `/api/items`, `/api/customers`, and `/api/stats`.

**Outcome:** The probe endpoint is no longer undecided: it is fixed by the Docker image's loopback/listening-port contract. The final native AMD64 image is 72.1 MB, runs as UID 1654, and its container reports healthy. The temporary validation container was removed after the check, releasing host port 5081.

## 08 - Document refactor state, health checks, and NuGet audit warning

**Instruction:** Document the refactoring approach and current state under `ai-journey`, explain how `/healthz` is executed by the container runtime, and explain and fix warning NU1900 on both new applications.

**Commands / actions:** Read the current journal and inspected the hosts, catalogs, health-check executable, Dockerfile, and tests. Created `ai-journey/refactoring-notes.md` with the refactoring boundary, configuration contract, image build/run procedure, health lifecycle, validation evidence, deferred work, and package-audit guidance. Confirmed `api.nuget.org` is the only configured package source; a restricted-shell request failed while a direct machine-level HTTPS request returned HTTP 200. Attempted the Microsoft-recommended HTTP-cache clear, but did not run it because permission for the machine-level cache change was declined.

**Outcome:** The warning is diagnosed as unavailable NuGet vulnerability metadata, not an application issue. Audit remains enabled and the notes prescribe retrying and clearing only NuGet's HTTP cache once network access is available, rather than suppressing the warning. The health check is documented as a Docker-engine command that runs inside the container; Kubernetes will require explicit native probes.

## 09 - Add practical Linux and Windows API validation commands

**Instruction:** Stop pursuing NU1900 because the intended validation machine lacks .NET, and provide API validation instructions for the Linux Docker container and Windows application through `dotnet run`.

**Commands / actions:** Inspected the concrete route implementations. Added Linux Docker build/run, health-status, endpoint-invocation, logs, and cleanup commands to the refactoring notes. Added Windows `dotnet run` and endpoint commands, with assertions for PNG rendering and printer-list behavior.

**Outcome:** Linux can be validated without a host .NET installation because the container carries the runtime. The current Windows supplement cannot be started by `dotnet run` until a Windows host has a .NET 8 SDK; its future Windows-container follow-up remains the alternative.

## 10 - Add Helm delivery bonus, Kind runbook, and Azure deployment direction

**Instruction:** Create Helm deployment templates for development, QAT, and production mocks with distinct connection-string values; add a build/load/install/readiness/rollback runbook for Kind; and record the planned Azure topology.

**Commands / actions:** Created a self-contained `helm/jtldemo-linux` chart with Deployment, Service, helpers, values, three initial environment overlays, and startup/readiness/liveness probes. Added the runbook with Docker Buildx, Kind image-load, Helm lint/template/install, API checks, failure diagnosis, and rollback instructions. Added the planned Azure model to the refactoring notes and plan. Checked local tool availability: `kubectl` is present, but Helm and Kind are not installed, so chart rendering and cluster deployment remain an explicit external validation step.

**Outcome:** This initial three-overlay design was superseded by the later two-mode local-dev/production chart. The Azure split remains a future architecture decision, not an implemented cloud deployment.

## 11 - Relocate the runbook and specify production secret controls

**Instruction:** Move the runbook into `deliverables`, make it deploy one selected environment rather than all three, provide placeholder cleanup syntax, and expand the cloud-adoption plan with container-secret and production Helm hardening guidance.

**Commands / actions:** Moved the runbook to `deliverables/RUNBOOK.md`; replaced the three-install sequence with a selected environment/release/namespace overlay; and replaced cleanup enumeration with `helm uninstall jtldemo-<env> --namespace jtldemo-<env>`. Expanded the plan and refactoring notes with the intended Azure Key Vault, workload identity, Kubernetes Secret, `secretKeyRef`, RBAC, rotation, schema-validation, and literal-secret avoidance strategy.

**Outcome:** The local runbook now exercises exactly one environment at a time. The future Helm security upgrade is explicit: only Kind uses mock literals; production receives connection strings through a least-privilege, externally managed container-secret reference.

## 12 - Add local-dev and Azure production Helm modes

**Instruction:** Enhance the Helm chart with supported local-dev and production modes, including an Azure Load Balancer-backed ingress architecture, horizontal pod scaling, and Azure Key Vault CSI secret references; keep the runbook focused on local-dev initialization only.

**Commands / actions:** Replaced the three mock overlays with `values-local-dev.yaml` and `values-production.yaml`. Added production-mode rendering guards, Ingress, HPA, ServiceAccount/workload-identity wiring, Key Vault `SecretProviderClass`, CSI volume, and `secretKeyRef` configuration source. Added a chart README describing the external AKS prerequisites and narrowed the runbook to local-dev values/release/namespace only. Reviewed current Microsoft and Kubernetes documentation for CSI workload identity, secret synchronization, Azure ingress/load-balancer ownership, and HPA behavior. A disposable Helm-container lint was attempted, but Docker Desktop's Linux engine was unavailable; static chart-contract checks and `git diff --check` passed instead.

**Outcome:** Local-dev remains the only runnable documented mode. Production is an Azure-ready chart skeleton that deliberately requires real platform provisioning and protected placeholder injection before use; it cannot fall back to a literal connection string.

## 13 - Compare Flux HelmRelease and Kustomization GitOps models

**Instruction:** Compare a Helm-plus-Flux release flow with a Flux-plus-Kustomize flow, especially image updates from CI/ACR and the risk of tying image changes to GitHub Releases.

**Commands / actions:** Researched official Flux, AKS, and Kubernetes documentation and captured the cited findings in `ai-journey/flux-gitops-research.md`.

**Outcome:** An ACR push alone does not restart workloads. Flux must discover it, select it through an image policy, commit the changed image reference to Git, then reconcile a HelmRelease or Kustomization; the resulting Pod-template change triggers Kubernetes rollout. Both approaches support the same GitOps image automation. The recommended fit is Flux HelmRelease over the existing chart, with chart versions reserved for chart contract changes, immutable image digests in GitOps state, auto-promotion only in development, and reviewed digest promotion to QAT/production.

## 14 - Confirm the staged Flux promotion policy

**Instruction:** Reword the GitOps plan so Flux patches the newest eligible image into AKS development immediately, while QAT and production HelmReleases remain pinned and receive the same thoroughly tested image by promotion.

**Commands / actions:** Revised the GitOps decision and next step to distinguish automatic development image-digest updates from reviewed QAT/production promotion.

**Outcome:** The future policy is explicit: Flux image automation updates development only; QAT and production keep pinned chart/image versions and promote the identical immutable digest after prior-environment validation.

## 15 - Independently evaluate the refactor decisions

**Instruction:** Review `ai-journey/refactoring-notes.md` from another perspective, evaluate the refactor decisions, and suggest corrections or improvements.

**Commands / actions:** Read the journaling and codebase-design skills, current journals, task requirements, application source/tests, Dockerfile, Helm chart, and deliverables. Re-ran both new solution tests with `--configuration Release --no-restore`; ran Helm lint/template against local-dev. Verified a proposed helper-definition correction in a temporary chart copy and inspected production rendering there. Checked official Docker, Kubernetes, Helm, and Microsoft documentation for operational claims. Added `ai-journey/refactoring-review.md` and updated the next steps.

**Outcome:** Endorsed the scoped Linux split while prioritizing a confirmed Helm parse failure, unreliable image/rollback instructions, missing HTTP-level Windows evidence, blank-configuration validation, and the incomplete deliverable README. Linux tests passed 2/2 and Windows tests 1/1 with NU1900 warnings. The temporary helper correction passes local lint and renders both modes under Helm 4.1.4, but production placeholders are accepted. Flagged the printer discovery-to-job-worker assumption and documented printing API support constraints. Product source and repository deployment files were not changed during this review. The user subsequently accepted the corrections; implementation and final scope are recorded below.

## 16 - Apply the accepted refactor corrections

**Instruction:** Apply the reviewed corrections, rerun the installed Helm linter, refresh package restore metadata, and stop repeating the earlier package-source diagnosis.

**Commands / actions:** Corrected Helm helper definitions, HPA replica ownership, image digest rendering, production input schema/guards, and Linux AMD64 scheduling. Pinned both base-image digests. Tightened both hosts' configuration validation and added a focused API smoke script. Updated the user's README draft while retaining its Windows hosting trade-offs; corrected runbook image identities, rollback selection, Helm 3/4 failure flags, and current refactoring notes. Ran fresh audited restores through the machine network, both solution tests, API smoke checks, Docker build/run verification, and direct Helm lint.

**Outcome:** Local-dev Helm lint passes with Helm 4.1.4. Linux tests pass 2/2 and Windows tests 1/1; both hosts pass missing/empty/whitespace rejection and retained/excluded HTTP route checks, including a decodable Windows PNG and printer-array response. The rebuilt image is healthy, runs as UID 1654, and serves all five Linux routes. Fresh audited restores succeed without suppression. Kind deployment/rollback and Azure production remain future validation; no cloud resources were changed.

## 17 - Keep Helm verification minimal

**Instruction:** Remove the additional Helm template sanity tests; keep the task working without overengineering.

**Commands / actions:** Removed the newly added Helm validation script and synthetic production fixture, removed their documentation references, and retained direct `helm lint` plus the ordinary runbook rendering command.

**Outcome:** No Helm sanity-test suite remains. Direct local-dev lint passes; the focused API smoke script remains to establish the required Linux/Windows behavior.

## 18 - Attempt local-dev Kind deployment

**Instruction:** Test the implemented Helm local-dev deployment after installing a Kind cluster; then update the RUNBOOK to make image loading into the running Kind cluster explicit, without adding a separate journal recap.

**Commands / actions:** Read the refactoring notes, local-dev chart values, chart runbook, and current journal. Confirmed Helm 4.1.4 lint passes with `values-local-dev.yaml` and rendered the expected Service and one-replica Deployment. Checked the active Kubernetes configuration, cluster reachability, nodes, and Docker API access before attempting installation. Revised `deliverables/RUNBOOK.md` to separate image build, Kind start/reuse, node-context verification, and post-start `kind load docker-image` steps.

**Outcome:** Deployment could not start from this shell: `kubectl` has no current context and falls back to an unavailable `localhost:8080` API; Docker cannot access its named-pipe API. The runbook now explicitly loads the newly built tagged image after the `jtldemo` cluster starts (or is reused), and verifies the Kind context before Helm installation.
