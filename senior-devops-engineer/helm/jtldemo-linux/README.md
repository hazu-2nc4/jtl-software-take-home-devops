# JtlDemo Linux Helm chart

This chart supports two deployment modes selected by values files:

| Mode | Values file | Purpose |
| --- | --- | --- |
| `local-dev` | `values-local-dev.yaml` | Runnable Kind deployment using `Server=local-dev`; no ingress, HPA, or Key Vault integration. |
| `production` | `values-production.yaml` | Azure/AKS skeleton only. It renders Ingress, HPA, workload-identity ServiceAccount, Key Vault `SecretProviderClass`, CSI volume, and `secretKeyRef` wiring. |

The production skeleton deliberately contains `REPLACE_ME` placeholders. It requires an AKS cluster with OIDC/workload identity, the Azure Key Vault Secrets Store CSI add-on, a metrics API for HPA, an ingress controller, and an Azure Load Balancer owned by the ingress-controller platform layer. The application Service remains `ClusterIP`; the Ingress directs HTTP traffic to it.

Production never accepts a literal connection string. The chart requires `config.source: existingSecret`, mounts the CSI volume to trigger Key Vault synchronization, and reads `ConnectionStrings__JtlDemo` through a Kubernetes `secretKeyRef`. Do not create a plaintext Helm `Secret` or put actual Key Vault IDs, hostnames, or image tags in version-controlled production values.
