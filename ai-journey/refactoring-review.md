# Independent review of the Linux replatform refactor

Reviewed: 2026-09-07. Scope: the working tree at review time, `refactoring-notes.md`, the take-home requirements, application projects/tests, Dockerfile, Helm chart, and deliverables. The user subsequently accepted the corrections; the original findings below are retained as historical evidence.

## Implementation results - 2026-09-07

- Fixed Helm helper nesting, added digest rendering and production input validation, gave HPA ownership of replicas, and selected Linux AMD64 nodes. Chart contract version is 0.2.0. Direct local-dev lint passes with installed Helm 4.1.4.
- Removed the additional Helm sanity-test script and synthetic fixture at the user's request. Keep direct linting and the normal runbook rendering command; no Helm test suite is retained.
- Both hosts reject absent, empty, and whitespace configuration. The repeatable API smoke script passes for both hosts: Linux endpoints/exclusions, Windows PNG decoding and printer-array response, and valid environment configuration. Windows checks ran as `HOME-PC\vuman`; local evidence is `.cache/api-checks/60242de2385d4f73baddb07f5a101d6b/`.
- Fresh audited restores and both new solution tests pass: Linux 2/2 and Windows 1/1. No dependency-audit suppression was introduced.
- Pinned both Docker base digests and rebuilt `jtldemo-linux:review-20260907-a`. Docker reports image identity `sha256:0ad9c4d6f2bc551f95756f1c6f53613dd7d0aff6bd394d0409794ef637c573a4`, user 1654, and healthy status. All five Linux routes returned HTTP 200; the temporary container was removed.
- Updated the README, runbook, and refactoring notes: frozen legacy ownership, unique local image tags, previously verified rollback revisions, Helm 3/4 failure flags, corrected health timing, production metadata/secret ownership, and supported printing/renderer assessment before future hosting work.

Kind deployment and a live Helm rollback remain unexecuted; Kind is not installed on PATH. Azure provisioning, secret rotation, and worker design remain future work. The runbook now supplies the corrected rollback procedure without adding a separate automated Helm validation framework.

## Assessment

Keep the Linux refactor. Moving Stats, removing registry/Event Log startup coupling, retaining the Windows implementations, and using explicit module catalogs are appropriate for the requested slice. The strongest alternative perspective is to judge the work by preserved behavior and reproducible recovery before expanding its cloud topology.

The application isolation is stronger than the delivery evidence. The current Helm chart cannot render, rollback instructions can select the wrong revision, and the Windows test proves composition rather than working Windows endpoints. These deserve attention before more Azure or GitOps implementation.

## Decisions worth retaining

| Decision | Assessment and qualification |
| --- | --- |
| Independent Linux and Windows solutions | Good fit for the explicitly selected scope. Project references and target frameworks provide the important isolation; folders and `.dockerignore` reinforce it. |
| Shared `IApiModule` and explicit catalogs | Keep this small registration interface. It is portable across operating systems but deliberately coupled to ASP.NET routing. It is not a future queue-message or domain contract. No plugin discovery framework is needed. |
| Stats in the portable module list | Correctly distinguishes an assembly's historical location from an implementation's actual OS requirements. |
| Standard configuration and non-secret `_config` | Appropriate migration seam. Tighten validation as discussed below. Small duplicated host setup does not yet justify a new shared hosting framework. |
| Multi-stage, non-root distroless image and executable probe | Proportionate to the explicit container-health requirement. The publish stage depends on the test stage, so the normal final build includes the test gate. Keep the agreed fixed 8080 probe contract. |

## Corrections before submission

### 1. High: the Helm helper definitions are incorrectly nested

In `senior-devops-engineer/helm/jtldemo-linux/templates/_helpers.tpl`, line 10 closes the `if`, but `jtldemo-linux.fullname` remains open when another `define` begins at line 12. The closing `end` at line 23 belongs before that next definition.

Both current-tree `helm lint` and `helm template` fail with:

```text
parse error at (jtldemo-linux/templates/_helpers.tpl:12): unexpected <define> in command
```

Suggested correction: move the `{{- end }}` currently at line 23 to immediately after line 10. I applied only that correction to a temporary copy: local-dev lint passed and both modes rendered. The repository chart remains unchanged by this review.

Acceptance: lint and render with the documented Helm 3 version, then install and verify local-dev in Kind. The available executable here is Helm 4.1.4; successful template checks with it do not validate the runbook's Helm 3 lifecycle commands.

### 2. High: image identity and rollback instructions do not guarantee recovery

`deliverables/RUNBOOK.md:31` recommends rebuilding and reloading `jtldemo-linux:local` before an upgrade. If chart values and the Pod template remain the same, this does not trigger replacement Pods. It also loses a reliable association between a Helm revision and its executable content. Kubernetes triggers a Deployment rollout when its Pod template changes. [Deployment update semantics](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/#updating-a-deployment).

Suggested correction: use a distinct never-reused local tag per build, load that tag into Kind, and pass the same tag through `--set image.tag=...`. Retain two versions for the rollback exercise. For registry delivery, add an explicit `image.digest` option and render `repository@sha256:...`.

`deliverables/RUNBOOK.md:98` also says to choose the latest revision whose status is `deployed`. After a successful upgrade that later reveals a defect, that is usually the defective current revision. Choose a previously verified revision and image identity; its history status is commonly `superseded`. [Helm release history](https://helm.sh/docs/helm/helm_history/).

Acceptance: deploy A, deploy B, explicitly roll back to A, and verify the running image identity and endpoint behavior. A healthy response alone cannot distinguish two builds whose responses are identical.

### 3. High: Windows functionality is not established by the composition test

The Windows test only constructs modules and checks their names. It never calls GDI rendering or `PrinterSettings.InstalledPrinters`. A passing test therefore cannot substantiate that the retained capabilities work under the intended Windows identity.

The Linux Stats test similarly reads the static value `42`; deleting its route would leave both Linux tests green. This is an evidence gap, not evidence that the routes currently fail.

Suggested correction: retain the useful catalog test and exercise behavior through HTTP. Check Linux Stats JSON and the absence of Windows routes. On Windows, verify a decodable PNG and a successful printer-array response, allowing zero printers. These can be a few focused integration checks or a repeatable smoke script, consistent with the task's limited test scope.

Acceptance: record the Windows runtime identity and actual route results separately from build/composition results. Keep historical container observations labeled with date and image identity rather than presenting them as current-tree verification.

### 4. Medium: fail-fast configuration accepts empty and whitespace values

Both new hosts use `GetConnectionString("JtlDemo") ?? throw`. Only `null` is rejected. An empty configured value starts the host and returns `configured: false`; whitespace starts it and returns `configured: true`.

Suggested correction: validate with `string.IsNullOrWhiteSpace` and preserve the actionable, non-secret error. Exercise absent, empty, whitespace, and valid values. This intentionally tightens inherited validation behavior and should be documented as such.

Also clarify that this demo does not use the connection string to access a database. Its current validation proves configuration injection, not database readiness. Keep the user's fail-fast contract for this slice; a future standalone printer worker should require only configuration it actually consumes.

### 5. Medium: complete the requested reviewer-facing deliverables

`deliverables/README.md` currently contains only a heading. The take-home explicitly asks for a short decisions/trade-offs README and short observability/exclusion reflections. The long AI journey explains the work, but does not yet provide that concise handoff.

Suggested correction: finish the half-page README, link the build/run procedures, and add the requested reflections. Explain the frozen legacy reference, both maintained hosts, configuration, chosen Helm bonus, and production follow-ups. The task asks for one bonus, so a second pipeline implementation is not necessary.

## Reconsider the future topology before extending it

OS separation establishes where an implementation can execute; it does not alone establish that each capability needs a separately operated network service.

The current Documents module returns a small synchronous PNG. The Printers module lists installed printers; it does not submit a print job. A queue-backed worker adds new behavior: submission, durable state, retries, cancellation, and completion reporting. Treat that as a future product/interface decision, while retaining the user's selected worker direction as a proposal to validate.

For a future printing workflow, determine where printers and drivers actually live, which identity can access them, and what a retry means if the printer accepted a job but the acknowledgement was lost. Generic retry/idempotency language cannot by itself guarantee that paper is printed exactly once.

There is also a concrete compatibility issue to add to the notes: Microsoft documents `System.Drawing.Printing` as unsupported in Windows services and ASP.NET applications/services. Retaining this inherited implementation for the exercise is reasonable, but placing it in a Windows worker or container does not establish production support. Prove a supported printing adapter and execution model before selecting compute. This is a documented support constraint, not a claim that the existing endpoint must always fail. [Microsoft printing API guidance](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.printing?view=windowsdesktop-8.0).

For Documents, evaluate rendering fidelity, fonts, performance, and library constraints first. If a portable renderer satisfies those requirements, its adapter could initially run inside the Linux host. An independent rendering service is justified later by isolation, scaling, or ownership needs. App Service remains a compatibility candidate, not an outcome established by the refactor.

Before a real client cutover, define route ownership and authentication across the two hosts, and document that the Windows supplement is not the original complete API. Deferring that interface is acceptable for the explicitly scoped local delivery.

## Improvements to the production skeleton and notes

These are follow-ups; they should not delay repairing and proving the local slice.

- **Give HPA ownership of replicas.** `templates/deployment.yaml:25` always emits `spec.replicas`, even with autoscaling enabled. Render that field only when autoscaling is disabled. Fixed desired replicas can reset HPA-managed scale when reapplied; this is not evidence of constant controller contention in the current undeployed skeleton. [Kubernetes HPA migration guidance](https://kubernetes.io/docs/concepts/workloads/autoscaling/horizontal-pod-autoscale/#migrating-deployments-and-statefulsets-to-horizontal-autoscaling).
- **Distinguish production requirements from validation.** After the helper correction in the temporary copy, production renders with every `REPLACE_ME` value intact. `required` checks non-emptiness, not valid identities, DNS names, or deployed prerequisites. Add schema/explicit placeholder checks and negative rendering cases before claiming invalid production inputs are rejected. Requiring HPA and Ingress in this Azure preset is a topology choice, not a general property of production readiness.
- **Separate secrets from deployment metadata.** The chart README prohibits committing actual image tags, hostnames, and Key Vault IDs. These are not secret values by themselves, although organizational policy may classify them. A protected GitOps repository normally needs the deployable image identity and non-secret configuration. Correct this blanket prohibition so it agrees with the Flux proposal; continue excluding credentials and connection-string contents.
- **Make rotation operational.** The CSI mount plus synchronized Secret design is internally consistent. However, environment variables in existing Pods do not update when the Secret rotates. Name the owner and mechanism for a controlled restart and verify recovery before production use. [AKS secret rotation behavior](https://learn.microsoft.com/en-us/azure/aks/csi-secrets-store-configuration-options#enable-and-disable-auto-rotation).
- **Describe health accurately.** The current `/healthz` proves that HTTP handling responds. That is proportionate to these in-memory demo routes. Separate readiness from liveness when actual required dependencies appear. In the Docker notes, replace the guaranteed ten-second `starting` period with initialization grace: a successful probe can mark the container healthy during that period. Poll health with a deadline rather than assuming ten seconds is enough. [Docker health-check semantics](https://docs.docker.com/reference/dockerfile/#healthcheck).
- **Distinguish version selection from immutable content.** The Dockerfile selects explicit patch/OS/architecture tags, but tags can be republished. For reproducible releases, pin both base images by digest and define a reviewed update cadence. Schedule vulnerability auditing independently of cached image layers. [Docker base-image pinning guidance](https://docs.docker.com/build/building/best-practices/#pin-base-image-versions).
- **Make scheduling match the artifact.** The image is AMD64-only; production values select Linux but not architecture. Require an AMD64 node pool or add `kubernetes.io/arch: amd64` before using a cluster with mixed architectures. Multi-architecture image work is unnecessary for the current agreed target.

## Verification performed for this review

| Check | Current result |
| --- | --- |
| Linux `dotnet test --configuration Release --no-restore` | 2/2 passed; NU1900 warning emitted |
| Windows `dotnet test --configuration Release --no-restore` | 1/1 passed; NU1900 warnings emitted |
| Original chart: local-dev Helm lint and template | Both fail at `_helpers.tpl:12` |
| Temporary chart with only the proposed helper correction | Local-dev lint passes; local-dev and production render under Helm 4.1.4 |
| Production placeholder rejection | Not implemented: temporary corrected chart renders placeholders |
| Container rebuild, live HTTP checks, Kind deployment, actual rollback | Not performed in this review; Kind is not on PATH |

The successful .NET runs do not establish a successful vulnerability audit. No warning suppression, product-source edits, repository-chart fixes, deployment, or cloud changes were made. Suggested implementation order: repair chart parsing; make deployment/rollback reproducible; verify HTTP behavior on both OS targets; tighten configuration validation; finish the README/reflections; then revisit production topology.
