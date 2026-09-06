# Local-dev Kind deployment runbook

This runbook deploys the Linux Items, Customers, and Stats API to a local Kind cluster using only the runnable `local-dev` mode. It uses the non-secret mock `Server=local-dev` value only to prove that Helm supplies `ConnectionStrings__JtlDemo` correctly.

## Prerequisites

- Docker Desktop is running with Linux containers enabled.
- `kubectl`, `kind`, and Helm 3 are on `PATH`.
- A local Kind cluster named `jtldemo` exists, or you may create it below.

Verify the tools:

```powershell
docker version
kind version
helm version
kubectl version --client
```

## Build and load the image

Run the following from `senior-devops-engineer/`. `--load` adds the AMD64 image to Docker Desktop; `kind load` then makes the exact image available to every Kind node. Do not use a remote registry for this local workflow.

```powershell
docker buildx build --platform linux/amd64 --load --tag jtldemo-linux:local --file linux-app/Dockerfile .
kind get clusters
kind create cluster --name jtldemo
kind load docker-image jtldemo-linux:local --name jtldemo
```

If `kind get clusters` already lists `jtldemo`, skip `kind create cluster`. Whenever the local image is rebuilt with the same `local` tag, run `kind load docker-image` again before upgrading the release.

## Render and install local-dev

Set the fixed local-dev chart paths, release, and namespace:

```powershell
$release = 'jtldemo-local-dev'
$namespace = 'jtldemo-local-dev'
$values = './helm/jtldemo-linux/values-local-dev.yaml'
```

Render before changing the cluster. This catches missing values and shows the local mock connection-string value that will be created:

```powershell
helm lint ./helm/jtldemo-linux --values $values
helm template $release ./helm/jtldemo-linux --namespace $namespace --values $values
```

Install or upgrade local-dev. `--atomic --wait` waits for readiness. A failed upgrade returns to the last successful revision; a failed first install is cleaned up automatically.

```powershell
helm upgrade --install $release ./helm/jtldemo-linux --namespace $namespace --create-namespace --values $values --wait --timeout 2m --atomic
```

Do not use `values-production.yaml` in this runbook. It is an Azure skeleton with intentionally invalid placeholders and requires provisioned AKS add-ons and identities.

## Readiness and API verification

Verify the local-dev release and inspect the local mock mapping:

```powershell
kubectl --namespace $namespace rollout status "deployment/$release-jtldemo-linux" --timeout=2m
kubectl --namespace $namespace get pods,service
kubectl --namespace $namespace get deployment "$release-jtldemo-linux" -o jsonpath='{.spec.template.spec.containers[0].env[?(@.name=="ConnectionStrings__JtlDemo")].value}'
```

In a dedicated terminal, forward the Service port:

```powershell
kubectl --namespace $namespace port-forward "service/$release-jtldemo-linux" 8080:80
```

In another terminal, validate the API:

```powershell
$baseUrl = 'http://127.0.0.1:8080'
Invoke-RestMethod "$baseUrl/healthz"
Invoke-RestMethod "$baseUrl/api/_config"
Invoke-RestMethod "$baseUrl/api/items"
Invoke-RestMethod "$baseUrl/api/customers"
Invoke-RestMethod "$baseUrl/api/stats"
```

Expected results are HTTP 200 for every call, `configured: true`, three Items, two Customers, and Stats value `42`.

## Failure diagnosis and rollback

`--atomic` handles a failed deployment automatically. For a deployment that later becomes unhealthy, collect evidence first:

```powershell
kubectl --namespace $namespace get pods
kubectl --namespace $namespace describe deployment "$release-jtldemo-linux"
kubectl --namespace $namespace logs "deployment/$release-jtldemo-linux" --all-containers=true
helm history $release --namespace $namespace
```

To revert an existing release, identify the last revision with `STATUS` `deployed`, then use its revision number:

```powershell
helm rollback $release <last-good-revision> --namespace $namespace --wait --timeout 2m
kubectl --namespace $namespace rollout status "deployment/$release-jtldemo-linux" --timeout=2m
```

For a failed first install there is no previous revision to restore; `--atomic` removes the failed release. After correcting the image or values, repeat the `helm upgrade --install` command.

## Local cleanup

Remove the local-dev release only when it is no longer needed:

```powershell
helm uninstall jtldemo-local-dev --namespace jtldemo-local-dev
```
