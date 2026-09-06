# Local-dev Kind deployment runbook

This runbook deploys the Linux Items, Customers, and Stats API to a local Kind cluster using only the runnable `local-dev` mode. It uses the non-secret mock `Server=local-dev` value only to prove that Helm supplies `ConnectionStrings__JtlDemo` correctly.

## Prerequisites

- Docker Desktop is running with Linux containers enabled.
- `kubectl`, `kind`, and Helm 3 or 4 are on `PATH`.
- A local Kind cluster named `jtldemo` exists, or you may create it below.

Verify the tools:

```powershell
docker version
kind version
helm version
kubectl version --client
```

## Build the image

Run the following from `senior-devops-engineer/`. `--load` adds the AMD64 image to Docker Desktop. Do not use a remote registry for this local workflow.

```powershell
$imageTag = 'local-' + [guid]::NewGuid().ToString('N')
docker buildx build --platform linux/amd64 --load --tag "jtldemo-linux:$imageTag" --file linux-app/Dockerfile .
```

## Start Kind and load the image

Start (or reuse) the `jtldemo` cluster, then load the newly built image into its running nodes before invoking Helm:

```powershell
$clusterName = 'jtldemo'
if ($clusterName -notin (kind get clusters)) {
    kind create cluster --name $clusterName
}
$clusterContext = "kind-$clusterName"
kubectl --context $clusterContext get nodes
kind load docker-image "jtldemo-linux:$imageTag" --name $clusterName
```

`kind load` copies the exact image into every Kind node; building it in Docker Desktop alone is insufficient. Generate a new tag for every build and retain prior images in Kind for rollback. Every upgrade must pass that build's tag to Helm; loading an image under an unchanged tag does not trigger a Deployment rollout.

For a Docker-only smoke check, start the image and poll its declared health status with a deadline:

```powershell
$containerName = 'jtldemo-check-' + [guid]::NewGuid().ToString('N')
docker run --detach --name $containerName --publish 127.0.0.1:8080:8080 --env ConnectionStrings__JtlDemo=Server=local-dev "jtldemo-linux:$imageTag"
$deadline = (Get-Date).AddSeconds(90)
do {
    $health = docker inspect --format '{{.State.Health.Status}}' $containerName
    if ($health -eq 'healthy') { break }
    if ($health -eq 'unhealthy') { throw 'Container unhealthy; inspect docker logs' }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)
if ($health -ne 'healthy') { throw 'Health deadline exceeded; inspect docker logs' }
Invoke-RestMethod http://127.0.0.1:8080/healthz
docker rm --force $containerName
```

## Render and install local-dev

Set the fixed local-dev chart paths, release, and namespace:

```powershell
$release = 'jtldemo-local-dev'
$namespace = 'jtldemo-local-dev'
$values = './helm/jtldemo-linux/values-local-dev.yaml'
$clusterContext = 'kind-jtldemo'
$helmMajor = [int]((helm version --short) -replace '^v(\d+).*', '$1')
$failureFlag = if ($helmMajor -ge 4) { '--rollback-on-failure' } else { '--atomic' }
```

Render before changing the cluster. This catches missing values and shows the local mock connection-string value that will be created:

```powershell
helm lint ./helm/jtldemo-linux --strict --values $values --set-string "image.tag=$imageTag"
helm template $release ./helm/jtldemo-linux --namespace $namespace --values $values --set-string "image.tag=$imageTag"
```

Install or upgrade local-dev. Helm 4 uses `--rollback-on-failure`; Helm 3 uses `--atomic`. With readiness waiting, a failed upgrade returns to the previous successful revision, and a failed first install is cleaned up.

```powershell
helm upgrade --install $release ./helm/jtldemo-linux --kube-context $clusterContext --namespace $namespace --create-namespace --values $values --set-string "image.tag=$imageTag" --wait --timeout 2m $failureFlag
```

Do not use `values-production.yaml` in this runbook. It is an Azure skeleton with intentionally invalid placeholders and requires provisioned AKS add-ons and identities.

## Readiness and API verification

Verify the local-dev release and inspect the local mock mapping:

```powershell
kubectl --context $clusterContext --namespace $namespace rollout status "deployment/$release-jtldemo-linux" --timeout=2m
kubectl --context $clusterContext --namespace $namespace get pods,service
kubectl --context $clusterContext --namespace $namespace get deployment "$release-jtldemo-linux" -o jsonpath='{.spec.template.spec.containers[0].env[?(@.name=="ConnectionStrings__JtlDemo")].value}'
kubectl --context $clusterContext --namespace $namespace get pods -o jsonpath='{.items[*].status.containerStatuses[*].imageID}'
```

In a dedicated terminal, forward the Service port:

```powershell
kubectl --context kind-jtldemo --namespace jtldemo-local-dev port-forward service/jtldemo-local-dev-jtldemo-linux 8080:80
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

The selected failure flag handles failures detected during deployment. For a release that later becomes unhealthy, collect evidence first:

```powershell
$release = 'jtldemo-local-dev'
$namespace = 'jtldemo-local-dev'
$values = './helm/jtldemo-linux/values-local-dev.yaml'
$clusterContext = 'kind-jtldemo'

kubectl --context $clusterContext --namespace $namespace get pods
kubectl --context $clusterContext --namespace $namespace describe deployment "$release-jtldemo-linux"
kubectl --context $clusterContext --namespace $namespace logs "deployment/$release-jtldemo-linux" --all-containers=true
helm history $release --kube-context $clusterContext --namespace $namespace
```

Choose a previously verified revision and its image identity. Its history status will often be `superseded`; `deployed` identifies the current revision, which might be defective. After verifying version A, record its revision and image before deploying B:

```powershell
$goodRevision = (helm history $release --kube-context $clusterContext --namespace $namespace --output json | ConvertFrom-Json | Select-Object -Last 1).revision
$goodImage = kubectl --context $clusterContext --namespace $namespace get deployment "$release-jtldemo-linux" -o jsonpath='{.spec.template.spec.containers[0].image}'
$goodImageId = kubectl --context $clusterContext --namespace $namespace get pods -l "app.kubernetes.io/instance=$release" -o jsonpath='{.items[0].status.containerStatuses[0].imageID}'
```

Build/load version B using a new `$imageTag`, repeat the upgrade and API checks, then deliberately recover A:

```powershell
helm rollback $release $goodRevision --kube-context $clusterContext --namespace $namespace --wait --timeout 2m
kubectl --context $clusterContext --namespace $namespace rollout status "deployment/$release-jtldemo-linux" --timeout=2m
$restoredImage = kubectl --context $clusterContext --namespace $namespace get deployment "$release-jtldemo-linux" -o jsonpath='{.spec.template.spec.containers[0].image}'
$restoredImageId = kubectl --context $clusterContext --namespace $namespace get pods -l "app.kubernetes.io/instance=$release" -o jsonpath='{.items[0].status.containerStatuses[0].imageID}'
if ($restoredImage -ne $goodImage -or $restoredImageId -ne $goodImageId) { throw 'Rollback image identity differs from verified version A' }
```

Restart port-forwarding if its Pod was replaced, then repeat the API checks. Keep A's image available until this exercise succeeds. For a failed first install there is no previous revision to restore; after correcting the image or values, repeat the install command.

## Local cleanup

Remove the local-dev release only when it is no longer needed:

```powershell
helm uninstall jtldemo-local-dev --kube-context kind-jtldemo --namespace jtldemo-local-dev
```
