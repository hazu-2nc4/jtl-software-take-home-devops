### how you found and cut the coupling

What I did:

- Ask AI Agent to scan the code base, build dependency graph and sumamry the blast radius when updating an API - see `.cache\dependency-analysis\windows-migration-audit.md`
- From AI output, the agent also giving a suggestion on which refactor was appropriate

Based on the agent outputs + the test instruction -> I decided

- Keep `Documents`, `Printers` left as it is, separated this with `windows-app`

  - Printers is highly couped with Windows OS -> Plan to refactor this as Windows worker service create more control and managed
  - The future for the Documents will depend on the found of alternative cross-platform renderer that could exceed the current in comparison, reliable maintainer and output quality -> So it stayed with Printers in Windows application
- Slice `Items`, `Customers`, and `Stats` out of origin project and put it under `linux-app`, provide `Dockerfile` located in the same directory suppport building Docker application image

  - Dockerfile separated to 4 stages: restore, test, publish and final runtime image
  - build and test used the `dotnet:sdk-*` enable all development features, the final (production-ready state) couped under `azurelinux3.0-distroless-extra` based (the extra provide additional dependencies to ease the work with cultures, dates, currencies,...) which contains only the crutial dependencies for the application to work, no extra, unsed dependencies bundled.

## Trade-Offs

### 1 - Documents -> PaaS + Printers -> Windows worker >< AKS windows-node pool

AKS manage 2 different node-pool architecture create more complexity

- Increaate manage effort on Windows OS nodepool, life-cycle
- bring application to Windows container image may create more issues
- May overlap with existing VM printer server - create additional cost

-> Go with (inexisted) VM worker for best printer driver supported and direct network connectivity

### 2 - Configuration + secrets

- Registry + Event Log startup blocked Linux -> replace with standard configuration and logging
- Both hosts read `ConnectionStrings__JtlDemo` -> stop startup if missing, empty or whitespace
- `_config` returns only a boolean -> no connection string exposed
- Local task uses mock values and in-memory data -> does not prove database connectivity

-> Production reads from an external Kubernetes Secret; no real credentials in Git or Helm values

### 3 - Container + deployment

- Linux AMD64 image, SDK/runtime pinned by digest -> keep the build inputs reproducible
- Distroless runtime has no shell -> use an executable HTTP health probe
- Run as non-root; Helm adds resource limits, read-only filesystem and native Kubernetes probes
- Pick Helm as the bonus -> keep validation simple with direct `helm lint`
- New image tag for each local build -> trigger rollout and retain the previous image for rollback
- Roll back to a previously verified revision -> the current `deployed` revision may be the broken one

-> Build, run, verify and rollback steps: [RUNBOOK.md](RUNBOOK.md)


## Future Azure setup

Current chart is a skeleton -> no Azure resources provisioned

- Linux image -> ACR -> AKS Linux node pool
- Platform setup -> ingress/TLS, workload identity, Key Vault/CSI and monitoring
- Secret rotation -> restart Pods and verify access before retiring the old credential
- Future pipeline -> isolated self-hosted runners where private-network access or Windows execution is needed
- Flux `HelmRelease` -> automatically update development; promote the same tested image digest to QAT and production by reviewed change

Windows follow-up:

- Printers currently lists installed printers only -> define job submission, retries and status before building the worker
- Prove unattended printing support, driver access and network/identity permissions -> putting it on Windows alone is not enough
- Documents -> compare renderer output, fonts, performance and maintenance first; a suitable portable renderer can join the Linux host before considering separate hosting


## Reflection

#### Observability

- Add structured logs + request tracing -> follow failures across requests
- Track latency, error rate and unavailable replicas -> alert on sustained service impact
- Add dependency readiness when real dependencies exist -> keep liveness independent so a database outage does not cause restart loops

#### Exclusion list

- Documents depends on GDI; Printers depends on Windows-installed printers -> both stay in the Windows supplement
- Verify actual HTTP behavior -> decode the preview PNG and check the printer response, not just module names
- Cloud direction stays conditional on the Windows follow-up above
