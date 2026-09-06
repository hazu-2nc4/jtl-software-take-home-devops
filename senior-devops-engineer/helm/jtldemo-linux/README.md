# JtlDemo Linux Helm chart

This chart supports two deployment modes selected by values files:

| Mode | Values file | Purpose |
| --- | --- | --- |
| `local-dev` | `values-local-dev.yaml` | Runnable Kind deployment using `Server=local-dev`; no ingress, HPA, or Key Vault integration. |
| `production` | `values-production.yaml` | Azure/AKS skeleton only. It renders Ingress, HPA, workload-identity ServiceAccount, Key Vault `SecretProviderClass`, CSI volume, and `secretKeyRef` wiring. |

The production skeleton deliberately contains `REPLACE_ME` placeholders and will not render until they are replaced. The schema validates types, digest format, identity GUIDs, hostnames, and configuration mode; template guards reject remaining placeholders and inverted HPA limits. This checks input shape, not whether Azure resources exist. Production requires an AKS cluster with OIDC/workload identity, the Azure Key Vault Secrets Store CSI add-on, a metrics API for HPA, an ingress controller, and an Azure Load Balancer owned by the ingress-controller platform layer. These are requirements of this Azure preset. The application Service remains `ClusterIP`.

Production never accepts a literal connection string. The chart requires `config.source: existingSecret`, mounts the CSI volume to trigger Key Vault synchronization, and reads `ConnectionStrings__JtlDemo` through a Kubernetes `secretKeyRef`. Keep secret contents and credentials out of Git and Helm values. Non-secret image digests, hostnames, and resource IDs may be versioned in the protected GitOps repository, subject to organizational policy.

`image.digest` takes precedence over `image.tag` and is required in production; local builds should use a fresh tag per build. When HPA is enabled the Deployment omits `spec.replicas`. Both modes select Linux AMD64 nodes, matching the image. The supported chart contract is version 0.2.0.

Validate local-dev with `helm lint . --values values-local-dev.yaml` from this directory. The runbook supports Helm 3 and 4 failure-handling flags. There is no separate Helm test suite.

Before production activation, the platform owner must enable CSI rotation, provision TLS and traffic policy, and validate load and availability settings. The application delivery owner must roll Pods after a synchronized Secret changes: an initial manual procedure is `kubectl --context <aks-context> --namespace <namespace> rollout restart deployment/<deployment>`, followed by `rollout status` and authenticated smoke checks. Do not retire the old credential until all Pods are verified. Test rotation in QAT and then automate the same controlled restart through a reviewed secret-change controller; environment variables do not refresh in existing Pods.

Review SDK/runtime base digests monthly and on security advisories, rebuild, and rerun the API/container checks before promotion. Run fresh audited restores on that schedule independently of Docker's cached restore layer. No scheduler or cloud controller is installed by this chart.
