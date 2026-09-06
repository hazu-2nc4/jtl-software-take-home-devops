## Ground rules

* **Time box.** Each task is scoped to roughly  **2 to 4 hours** . Going over that is optional and not expected. A smaller, well-executed solution beats a rushed, complete one.
* **Scope.** Build the core the task asks for. Do not gold-plate. Each task lists explicitly what you do **not** need to spend time on.
* **Tools.** Use the languages, frameworks, and libraries stated in your task. Where the task leaves a choice open, pick what you would pick at work and say why.
* **AI tools.** Using AI assistants is allowed and encouraged. We are interested in *how* you use them, not whether you do. Documenting this is a required deliverable and a significant part of the evaluation (see **AI journey** below).
* **Purpose.** These tasks exist only to evaluate your work. Nothing you submit is used in our products.

## What to build

### Required

1. **Make the service run without Windows.**
2. **Package it as a Linux container**

**Bonus (pick one, not both)**

If you have time, do **one** of the following. Either is enough; there is no need to do both.

* **Ship it through a pipeline.** A GitHub Actions workflow that builds, tests, and publishes the image.
* **Prepare it for Kubernetes.** A Helm chart that deploys the container to a local cluster (`kind`/`minikube`).

Do what you can well and note what you would do next. A clean slice and a running Linux image is a strong submission on its own.

### Deliverables

* The refactored source, the `Dockerfile`, and your bonus piece if you did one (the workflow or the Helm chart).
* A short **README** (about half a page) with your key decisions and trade-offs: how you found and cut the coupling, what you kept in the Windows supplement and why, how config and secrets are handled, and **what would change for a real Azure production setup** (self-hosted runners, secret storage, registry, observability).
* A short  **runbook** : how to build the image, run it (and deploy it, if you did the bonus), verify `/healthz`, and roll back.
* A short **reflection** (2 to 3 sentences or bullet points each):
  * **Observability** : what telemetry, metrics, and alerting you would add, and why.
  * **The exclusion list** : what you left out of the Linux image and how you would serve those Windows-only capabilities in a cloud world.
* An **`ai-journey/` folder** documenting how you used AI: the plan you worked from, your key prompts, the tools/models/skills/MCP servers you used, and where you overrode the output.
